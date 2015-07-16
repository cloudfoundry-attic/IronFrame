#include <windows.h>
#include <Psapi.h>
#include <Aclapi.h>

#include <iostream>
#include <string>
#include <vector>
#include <set>

using namespace std;

HANDLE hGuardJob;
HANDLE hDischargeEvent;

const size_t processesIdListLength = 16384;
DWORD processes[sizeof(DWORD) * processesIdListLength];

JOBOBJECT_BASIC_PROCESS_ID_LIST* processesInJobBuffer;


// The rate to check for processes
long checkRateMs = 100;

HANDLE GetParentJobObjectHandle(wstring &name)
{
	HANDLE hJob = OpenJobObject(JOB_OBJECT_ALL_ACCESS, false, name.c_str());

	if (hJob == NULL)
	{
		wclog << L"Could not open Job Object: " << name << " . Terminating guard." << endl;

		return NULL;
	}

	return hJob;
}

bool ParentJobExists(wstring &containerId) {
	HANDLE hParentJob = GetParentJobObjectHandle(containerId);

	if (hParentJob == NULL)
	{
		return false;
	}

	if (!CloseHandle(hParentJob)){
		wclog << L"Error on CloseHandle(hParentJob)." << endl;

		exit(GetLastError());
	}

	return true;
}

HANDLE CreateGuardJobObject(wstring &name)
{
	HANDLE hJob = CreateJobObject(NULL, (wstring(L"Global\\") + name).c_str());

	if (hJob == NULL)
	{
		wclog << L"Could not open Safety Job Object: " << name << " . Terminating guard." << endl;

		exit(GetLastError());
	}

	if (GetLastError() == ERROR_ALREADY_EXISTS)
	{
		// Job already existed
		wclog << L"Unexpected state. Guard Job Object already exists: " << name << " . Terminating guard." << endl;

		exit(-2);
	}
	else
	{
		// Job was created
		wclog << L"Created new Job Object: " << name << endl;

		// Activate JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE for Guard Job Object

		JOBOBJECT_EXTENDED_LIMIT_INFORMATION  jobOptions;
		ZeroMemory(&jobOptions, sizeof(jobOptions));
		jobOptions.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
		BOOL res = SetInformationJobObject(hJob, JobObjectExtendedLimitInformation, &jobOptions, sizeof(jobOptions));

		if (res == false)
		{
			wclog << L"Could not set JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE for job: " << name << " . Terminating guard." << endl;
			exit(GetLastError());
		}
	}

	return hJob;
}

HANDLE CreateEphemeralJobObject()
{
	HANDLE hJob = CreateJobObject(NULL, NULL);
	if (hJob == NULL){
		return hJob; // MAYBE??????
	}

	// Activate JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE for Guard Job Object

	JOBOBJECT_EXTENDED_LIMIT_INFORMATION  jobOptions;
	ZeroMemory(&jobOptions, sizeof(jobOptions));
	jobOptions.BasicLimitInformation.LimitFlags = JOB_OBJECT_LIMIT_KILL_ON_JOB_CLOSE;
	SetInformationJobObject(hJob, JobObjectExtendedLimitInformation, &jobOptions, sizeof(jobOptions));

	return hJob;
}

void EnsureProcessIsInJob(HANDLE hJob, HANDLE hProcess){
	BOOL processInJob = false;

	if (!IsProcessInJob(hProcess, hJob, &processInJob)){
		wclog << L"Error on IsProcessInJob." << endl;
		return;
	}

	if (!processInJob){
		// If the process is not in the Job Object put it back in
		if (!AssignProcessToJobObject(hJob, hProcess)){
			DWORD errorCode = GetLastError();

			TerminateProcess(hProcess, -1);

			// ERROR_ACCESS_DENIED is returned when the Process is not running anymore
			if (errorCode != ERROR_NOT_ENOUGH_QUOTA || errorCode != ERROR_ACCESS_DENIED){
				// TODO: fail on errors after IronFrame integration
				// kill the process first and then fail fast :?
				wclog << L"Error on AssignProcessToJobObject." << endl;

				return;
			}
		}
	}
}

int PutProcessBackInTheJob(wstring &containerId, HANDLE hJob, PSID userSid)
{
	DWORD bufferSize = (DWORD)sizeof(processes);
	DWORD cbBytesReturned;

	// TODO: is CreateToolhelp32Snapshot better?
	if (!EnumProcesses(processes, bufferSize, &cbBytesReturned)) {
		wclog << L"Error on EnumProcesses." << endl;
		return -1;
	}

	if (cbBytesReturned == bufferSize){
		wclog << L"Error on EnumProcesses. Buffer may be too small." << endl;
		return -1;
	}

	int numUserSidProcesses = 0;

	size_t numProcesses = cbBytesReturned / sizeof(DWORD);

	HANDLE hProcess = NULL;
	HANDLE hProcessToken = NULL;

	for (size_t i = 1; i < numProcesses; i++){

		DWORD pid = processes[i];

		if (hProcess != NULL){
			CloseHandle(hProcess);
			hProcess = NULL;
		}

		hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
		if (hProcess == NULL) {
			// Is this safe? Could container process change it's permissions
			continue;
		}


		if (hProcessToken != NULL){
			CloseHandle(hProcessToken);
			hProcessToken = NULL;
		}

		// get the process user name
		if (!OpenProcessToken(hProcess, TOKEN_QUERY, &hProcessToken)){
			wclog << L"Error on OpenProcessToken." << endl;
			continue;
		}

		char tokenUserBuffer[1024];
		PTOKEN_USER tokenUser = (PTOKEN_USER)tokenUserBuffer;
		DWORD dwSize;

		if (!GetTokenInformation(hProcessToken, TokenUser, tokenUser, sizeof(tokenUserBuffer), &dwSize)){
			wclog << L"Error on GetTokenInformation." << endl;
			continue;
		}

		if (EqualSid(userSid, tokenUser[0].User.Sid)){
			// we need to ensure here that hProcess is inside hJob
			numUserSidProcesses++;

			BOOL processInJob = false;

			if (!IsProcessInJob(hProcess, hJob, &processInJob)){
				wclog << L"Error on IsProcessInJob." << endl;
				continue;
			}

			if (!processInJob){
				HANDLE hParentJob = GetParentJobObjectHandle(containerId);
				if (hParentJob != NULL){
					EnsureProcessIsInJob(hParentJob, hProcess);

					if (!CloseHandle(hParentJob)){
						wclog << L"Error on CloseHandle(hParentJob)." << endl;
						exit(GetLastError());
					}

					EnsureProcessIsInJob(hJob, hProcess);
				}
				else {
					HANDLE ephemeralJob = CreateEphemeralJobObject();
					AssignProcessToJobObject(ephemeralJob, hProcess);

					TerminateProcess(hProcess, -1);

					CloseHandle(ephemeralJob);
				}
			}
		}
	}

	if (hProcess != NULL){
		CloseHandle(hProcess);
	}
	if (hProcessToken != NULL){
		CloseHandle(hProcessToken);
	}

	return numUserSidProcesses;
}


bool Discharged()
{
	DWORD res = WaitForSingleObject(hDischargeEvent, 0);
	if (res == WAIT_OBJECT_0)
	{
		return true;
	}
	else if (res == WAIT_TIMEOUT)
	{
		return false;
	}
	else
	{
		return true;
	}
}

// Similar code here: https://chromium.googlesource.com/chromium/chromium/+/master/sandbox/src/acl.cc
void SetDefaultDacl()
{
	HANDLE curProc = GetCurrentProcess();
	HANDLE curToken;
	BOOL res = OpenProcessToken(curProc, TOKEN_ADJUST_DEFAULT | TOKEN_READ, &curToken);

	if (res == false)
	{
		wclog << L"Error on OpenProcessToken." << endl;
		exit(GetLastError());
	}

	DWORD defDaclLen;
	GetTokenInformation(curToken, TokenDefaultDacl, NULL, 0, &defDaclLen);


	TOKEN_DEFAULT_DACL *defaultDacl = (TOKEN_DEFAULT_DACL *)malloc(defDaclLen);

	res = GetTokenInformation(curToken, TokenDefaultDacl, defaultDacl, defDaclLen, &defDaclLen);
	if (res == false)
	{
		wclog << L"Error on GetTokenInformation." << endl;
		exit(GetLastError());
	}


	EXPLICIT_ACCESS eaccess;
	BuildExplicitAccessWithName(&eaccess, L"BUILTIN\\Administrators", GENERIC_ALL, GRANT_ACCESS, 0);

	PACL newDacl = NULL;
	DWORD dres = SetEntriesInAcl(1, &eaccess, defaultDacl->DefaultDacl, &newDacl);
	if (dres != ERROR_SUCCESS)
	{
		wclog << L"Error on SetEntriesInAcl." << endl;
		exit(dres);
	}

	TOKEN_DEFAULT_DACL newDefaultDacl = { 0 };
	newDefaultDacl.DefaultDacl = newDacl;
	res = SetTokenInformation(curToken, TokenDefaultDacl, &newDefaultDacl, sizeof(newDefaultDacl));
	if (res == false)
	{
		wclog << L"Error on SetTokenInformation." << endl;
		exit(GetLastError());
	}

	LocalFree(newDacl);
	free(defaultDacl);
}

int wmain(int argc, wchar_t **argv)
{
	if (argc != 3)
	{
		wcerr << L"Usage: CloudFoundry.WindowsPrison.Guard.exe <username> <parent_job_object_name>" << endl;
		exit(-1);
	}

	// Get arguments
	wstring username(argv[1]);
	wstring containerId(argv[2]);

	SetDefaultDacl();

	wstring dischargeEventName = wstring(L"Global\\discharge-") + username;

	hGuardJob = CreateGuardJobObject(username + L"-guard");

	hDischargeEvent = CreateEvent(NULL, true, false, dischargeEventName.c_str());

	char userSidBuffer[1024];
	PSID userSid = userSidBuffer;
	DWORD userSidSize = sizeof(userSidBuffer);
	wchar_t domainName[1024];
	DWORD domainNameSize = sizeof(domainName);
	SID_NAME_USE sidUse;

	if (!LookupAccountName(NULL, username.c_str(), userSid, &userSidSize, domainName, &domainNameSize, &sidUse)){
		exit(GetLastError());
	}

	bool run = true;

	for (;;)
	{
		if (Discharged())
		{
			wclog << L"Guard is discharged. Shutting down." << endl;
			run = false;
		}

		if (!ParentJobExists(containerId)){
			run = false;
		}

		int numUserSidProcesses = PutProcessBackInTheJob(containerId, hGuardJob, userSid);

		if (!run && numUserSidProcesses == 0)
		{
			break;
		}

		if (!run) {
			TerminateJobObject(hGuardJob, -1);
		}

		if (run)
		{
			Sleep(checkRateMs);
		}
	}

	CloseHandle(hGuardJob);

	if (hDischargeEvent != NULL)
	{
		CloseHandle(hDischargeEvent);
	}

	return 0;
}
