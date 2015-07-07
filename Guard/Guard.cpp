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

size_t processesIdListLength = 16384;
JOBOBJECT_BASIC_PROCESS_ID_LIST* processesInJobBuffer;

// The rate to check for memory
long checkRateMs = 100;

HANDLE GetParentJobObjectHandle(wstring &name)
{
	HANDLE hJob = OpenJobObject(JOB_OBJECT_ALL_ACCESS, false, name.c_str());

	if (hJob == NULL)
	{
		wclog << L"Could not open Job Object: " << name << " . Terminating guard." << endl;

		exit(GetLastError());
	}

	return hJob;
}

void AssertParentJobExists(wstring &containerId) {
	HANDLE hParentJob = GetParentJobObjectHandle(containerId);
	if (!CloseHandle(hParentJob)){
		wclog << L"Error on CloseHandle(hParentJob)." << endl;

		exit(GetLastError());
	}
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

void EnsureProcessIsInJob(HANDLE hJob, HANDLE hProcess){
	BOOL processInJob = false;

	if (!IsProcessInJob(hProcess, hJob, &processInJob)){
		wclog << L"Error on IsProcessInJob." << endl;

		exit(GetLastError());
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

				exit(errorCode);
			}
		}
	}
}

void PutProcessBackInTheJob(wstring &containerId, HANDLE hJob, PSID userSid)
{
	size_t bufferSize = sizeof(DWORD) * 1024 * 1024;
	DWORD *processes = (DWORD*)malloc(bufferSize);

	if (processes == NULL) {
		wclog << L"Error on malloc." << endl;
		exit(-1);
	}

	DWORD cbNeeded;

	// TODO: increase the buff size if needed

	// TODO: is CreateToolhelp32Snapshot better?
	if (!EnumProcesses(processes, bufferSize, &cbNeeded)) {
		wclog << L"Error on EnumProcesses." << endl;

		exit(GetLastError());
	}

	if (cbNeeded == bufferSize){
		wclog << L"Error on EnumProcesses. Buffer to small." << endl;

		exit(-1);
	}

	size_t numProcesses = cbNeeded / sizeof(DWORD);

	for (size_t i = 1; i < numProcesses; i++){

		if (processes[i] != 0){
			DWORD pid = processes[i];

			HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, FALSE, pid);
			if (hProcess == NULL) {
				// Is this safe? Could container process change it's permissions
				continue;
			}


			// get the process user name
			HANDLE hProcessToken;
			if (!OpenProcessToken(hProcess, TOKEN_QUERY, &hProcessToken)){
				wclog << L"Error on OpenProcessToken." << endl;

				exit(GetLastError());
			}

			char tokenUserBuffer[1024];
			PTOKEN_USER tokenUser = (PTOKEN_USER)tokenUserBuffer;
			DWORD dwSize;

			if (!GetTokenInformation(hProcessToken, TokenUser, tokenUser, sizeof(tokenUserBuffer), &dwSize)){
				wclog << L"Error on GetTokenInformation." << endl;

				exit(GetLastError());
			}

			if (EqualSid(userSid, tokenUser[0].User.Sid)){
				// we need to ensure here that hProcess is inside hJob

				BOOL processInJob = false;

				if (!IsProcessInJob(hProcess, hJob, &processInJob)){
					wclog << L"Error on IsProcessInJob." << endl;

					exit(GetLastError());
				}

				if (!processInJob){
					HANDLE hParentJob = GetParentJobObjectHandle(containerId);
					EnsureProcessIsInJob(hParentJob, hProcess);

					if (!CloseHandle(hParentJob)){
						wclog << L"Error on CloseHandle(hParentJob)." << endl;

						exit(GetLastError());
					}

					EnsureProcessIsInJob(hJob, hProcess);
				}
			}
		}
	}

	free(processes);
}

void GetProcessIds(HANDLE hJob, vector<unsigned long> &processList)
{
	//Get a list of all the processes in this job.
	size_t listSize = sizeof(JOBOBJECT_BASIC_PROCESS_ID_LIST) + sizeof(ULONG_PTR)* processesIdListLength;

	if (processesInJobBuffer == NULL)
	{
		processesInJobBuffer = (JOBOBJECT_BASIC_PROCESS_ID_LIST*)LocalAlloc(
			LPTR,
			listSize);
	}

	if (processesInJobBuffer != NULL)
	{
		BOOL ret = QueryInformationJobObject(
			hJob,
			JobObjectBasicProcessIdList,
			processesInJobBuffer,
			(DWORD)listSize,
			NULL);

		if (!ret)
		{
			wclog << L"Error querying for JobObjectBasicProcessIdList. Terminating guard. Error code: " << GetLastError() << endl;
			exit(GetLastError());
		}
	}
	else
	{
		wclog << L"Could not allocate buffer for processes. Terminating guard." << endl;
		exit(GetLastError());
	}

	if (processesInJobBuffer->NumberOfAssignedProcesses > processesInJobBuffer->NumberOfProcessIdsInList) {
		if (processesInJobBuffer->NumberOfAssignedProcesses >= processesIdListLength) {
			wclog << L"Processes id list is to small. Doubling the list size." << endl;

			processesIdListLength *= 2;
			LocalFree(processesInJobBuffer);
			processesInJobBuffer = NULL;

			return GetProcessIds(hJob, processList);
		}
	}

	processList.resize(processesInJobBuffer->NumberOfProcessIdsInList);

	for (size_t i = 0; i < processesInJobBuffer->NumberOfProcessIdsInList; i++)
	{
		processList[i] = (unsigned long)processesInJobBuffer->ProcessIdList[i];
	}


}


PROCESS_MEMORY_COUNTERS GetProcessIdMemoryInfo(unsigned long processId)
{
	PROCESS_MEMORY_COUNTERS counters = {};

	HANDLE hProcess = OpenProcess(PROCESS_ALL_ACCESS, false, processId);

	if (hProcess == NULL)
	{
		wclog << L"Could not open Process Id: " << processId << endl;

		return counters;
	}

	ZeroMemory(&counters, sizeof(counters));

	GetProcessMemoryInfo(hProcess, &counters, sizeof(counters));

	CloseHandle(hProcess);

	return counters;
}

void GetProcessesMemoryInfo(vector<unsigned long> &processList, vector<PROCESS_MEMORY_COUNTERS> &counters)
{

	counters.resize(processList.size());
	for (size_t i = 0; i < processList.size(); i++)
	{
		PROCESS_MEMORY_COUNTERS counter = GetProcessIdMemoryInfo(processList[i]);
		counters[i] = counter;
	}
}

long long GetTotalKernelMemory(vector<PROCESS_MEMORY_COUNTERS> &counters)
{
	long long sum = 0;

	for (size_t i = 0; i < counters.size(); i++)
	{
		sum += counters[i].QuotaPagedPoolUsage + counters[i].QuotaNonPagedPoolUsage;
	}
	return sum;
}

long long GetTotalWorkingSet(vector<PROCESS_MEMORY_COUNTERS> &counters)
{
	long long  sum = 0;

	for (size_t i = 0; i < counters.size(); i++)
	{
		sum += counters[i].WorkingSetSize;
	}
	return sum;
}

//LPWSTR *argv;
//int argc;
//
//void GetCommandLineArgs()
//{
//	argv = CommandLineToArgvW(GetCommandLineW(), &argc);
//	if (NULL == argv)
//	{
//		wclog << L"CommandLineToArgvW failed\n";
//		exit(1);
//	}
//
//}
//
//int WINAPI wWinMain(HINSTANCE hInstance, HINSTANCE hPrevInstance, PWSTR pCmdLine, int nCmdShow)
//{
//	GetCommandLineArgs();
//

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
	if (argc != 3 && argc != 4)
	{
		wcerr << L"Usage: CloudFoundry.WindowsPrison.Guard.exe <username> <memory_bytes_quota> [<parent_job_object_name>]" << endl;
		exit(-1);
	}

	// Get arguments
	wstring username(argv[1]);
	wstring memoryQuotaString(argv[2]);
	wstring containerId;

	if (argc == 4){
		containerId = wstring(argv[3]);
	}
	else {
		containerId = wstring(argv[1]);
	}

	long memoryQuota = _wtol(memoryQuotaString.c_str());

	SetDefaultDacl();

	wstring dischargeEventName = wstring(L"Global\\discharge-") + username;

	hGuardJob = CreateGuardJobObject(username + L"-guard");

	hDischargeEvent = CreateEvent(NULL, true, false, dischargeEventName.c_str());

	vector<PROCESS_MEMORY_COUNTERS> counters;
	vector<unsigned long> processList;

	char userSidBuffer[1024];
	PSID userSid = userSidBuffer;
	DWORD userSidSize = sizeof(userSidBuffer);
	wchar_t domainName[1024];
	DWORD domainNameSize = sizeof(domainName);
	SID_NAME_USE sidUse;

	if (!LookupAccountName(NULL, username.c_str(), userSid, &userSidSize, domainName, &domainNameSize, &sidUse)){
		exit(GetLastError());
	}

	for (;;)
	{
		if (Discharged())
		{
			wclog << L"Guard is discharged. Shutting down." << endl;

			break;
		}

		AssertParentJobExists(containerId);

		PutProcessBackInTheJob(containerId, hGuardJob, userSid);

		if (memoryQuota > 0)
		{

			processList.clear();
			counters.clear();

			GetProcessIds(hGuardJob, processList);

			GetProcessesMemoryInfo(processList, counters);

			long long kernelMemoryUsage = GetTotalKernelMemory(counters);
			long long workingSetUsage = GetTotalWorkingSet(counters);
			long long totalMemUsage = kernelMemoryUsage + workingSetUsage;

			if (totalMemUsage >= memoryQuota)
			{
				// all hell breaks loose
				// TODO: consider killing the prison by setting the job memory limit to 0

				// TerminateJobObject is not aggresive enough to stop a fork bomb :?
				// Another alternative to exit is to CloseHandle the job object and then create it back again
				// TerminateJobObject(hParentJob, -1);

				wclog << L"Quota exceeded. Terminated Job: " << username << " at " << totalMemUsage << " bytes." << endl;

				exit(-66);
			}
		}

		// If waiting for multiple events is required use:
		// http://msdn.microsoft.com/en-us/library/windows/desktop/ms687003 or
		// http://msdn.microsoft.com/en-us/library/windows/desktop/ms687008
		Sleep(checkRateMs);
	}

	CloseHandle(hGuardJob);

	if (hDischargeEvent != NULL)
	{
		CloseHandle(hDischargeEvent);
	}

	return 0;
}
