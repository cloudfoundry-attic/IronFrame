using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading.Tasks;
using NSubstitute.Core;

namespace NSubstitute
{
    public static class NSubstituteExtensions
    {
        public static T Arg<T>(this CallInfo callInfo, int index)
        {
            return (T)callInfo.Args()[index];
        }

        public static T Arg<T>(this ICall call)
        {
            return call.GetArguments().OfType<T>().SingleOrDefault();
        }

        public static T Arg<T>(this ICall call, int index)
        {
            return (T)call.GetArguments()[index];
        }

        public static CallInfo Captured<T>(this T substitute, Expression<Action<T>> expr)
        {
            return Captured<T>(substitute, 0, expr);
        }

        public static CallInfo Captured<T>(this T substitute, int callNumber, Expression<Action<T>> expr)
        {
            var call = GetMatchingCalls(substitute, expr)
                .ElementAtOrDefault(callNumber);

            if (call == null)
                throw new Exception("Cannot find matching call.");

            var methodParameters = call.GetParameterInfos();
            var arguments = new Argument[methodParameters.Length];
            var argumentValues = call.GetArguments();

            for (int i = 0; i < arguments.Length; i++)
            {
                var methodParameter = methodParameters[i];
                var argumentIndex = i;

                arguments[i] = new Argument(methodParameter.ParameterType, () => argumentValues[argumentIndex], _ => { });
            }

            return new CallInfo(arguments);
        }

        public static void DidNotReceive<T>(this T instance, Action<T> action) where T : class
        {
            action(instance.DidNotReceive());
        }

        public static void DidNotReceiveWithAnyArgs<T>(this T instance, Action<T> action) where T : class
        {
            action(instance.DidNotReceiveWithAnyArgs());
        }

        static MethodInfo ExtractMethodInfo<T>(Expression<Action<T>> expr)
        {
            if (expr.Body.NodeType == ExpressionType.Call)
            {
                return ((MethodCallExpression)expr.Body).Method;
            }

            throw new Exception("Cannot find method.");
        }

        static IEnumerable<ICall> GetMatchingCalls<T>(T substitute, Expression<Action<T>> expr)
        {
            var router = SubstitutionContext.Current.GetCallRouterFor(substitute);
            var method = ExtractMethodInfo(expr);
            var calls = router.ReceivedCalls();
            return calls.Where(c => c.GetMethodInfo() == method);
        }

        public static void Received<T>(this T instance, Action<T> action) where T : class
        {
            action(instance.Received());
        }

        public static void Received<T>(this T instance, int callCount, Action<T> action) where T : class
        {
            action(instance.Received(callCount));
        }

        public static void Received<T>(this T instance, Func<T, object> action) where T : class
        {
            action(instance.Received());
        }

        public static void Received<T>(this T instance, int callCount, Func<T, object> action) where T : class
        {
            action(instance.Received(callCount));
        }

        public static IEnumerable<ICall> ReceivedCalls<T>(this T instance, Expression<Action<T>> expr) where T : class
        {
            return GetMatchingCalls(instance, expr);
        }

        public static void ReceivedWithAnyArgs<T>(this T instance, int callCount, Func<T, object> action) where T : class
        {
            action(instance.ReceivedWithAnyArgs(callCount));
        }

        /// <summary>
        /// Causes the specified call to return one of its own arguments, when arguments match.
        /// </summary>
        public static void ReturnsArgument<T>(this T instance, int argumentIndex)
        {
            instance.Returns(callInfo => callInfo[argumentIndex]);
        }

        public static void ReturnsTask<T>(this Task<T> instance, Func<T> returnThis, params Func<T>[] returnThese)
        {
            var taskOfReturnThese =
                returnThese.Select<Func<T>, Func<CallInfo, Task<T>>>(x => _ => Task.FromResult<T>(x()));

            instance.Returns(
                callInfo => Task.FromResult<T>(returnThis()),
                taskOfReturnThese.ToArray()
            );
        }

        public static void ReturnsTask<T>(this Task<T> instance, Func<CallInfo, T> returnThis, params Func<CallInfo, T>[] returnThese)
        {
            var taskOfReturnThese =
                returnThese.Select<Func<CallInfo, T>, Func<CallInfo, Task<T>>>(x => callInfo => Task.FromResult<T>(x(callInfo))).ToArray();

            instance.Returns(
                callInfo => Task.FromResult<T>(returnThis(callInfo)),
                taskOfReturnThese.ToArray()
            );
        }

        public static void ReturnsTask<T>(this Task<T> instance, T returnThis, params T[] returnThese)
        {
            var taskOfReturnThese = returnThese.Select(x => Task.FromResult<T>(x));

            instance.Returns(
                Task.FromResult<T>(returnThis),
                taskOfReturnThese.ToArray()
            );
        }

        public static void ReturnsTaskForAnyArgs<T>(this Task<T> instance, T returnThis, params T[] returnThese)
        {
            var taskOfReturnThese = returnThese.Select(x => Task.FromResult<T>(x));

            instance.ReturnsForAnyArgs(
                Task.FromResult<T>(returnThis),
                taskOfReturnThese.ToArray()
            );
        }

        public static void ReturnsTaskForAnyArgs<T>(this Task<T> instance, Func<T> returnThis, params Func<T>[] returnThese)
        {
            var taskOfReturnThese =
                returnThese.Select<Func<T>, Func<CallInfo, Task<T>>>(x => _ => Task.FromResult<T>(x())).ToArray();

            instance.ReturnsForAnyArgs(
                callInfo => Task.FromResult(returnThis()),
                taskOfReturnThese.ToArray()
            );
        }

        public static void ReturnsTaskForAnyArgs<T>(this Task<T> instance, Func<CallInfo, T> returnThis, params Func<CallInfo, T>[] returnThese)
        {
            var taskOfReturnThese =
                returnThese.Select<Func<CallInfo, T>, Func<CallInfo, Task<T>>>(x => callInfo => Task.FromResult<T>(x(callInfo))).ToArray();

            instance.ReturnsForAnyArgs(
                callInfo => Task.FromResult<T>(returnThis(callInfo)),
                taskOfReturnThese.ToArray()
            );
        }

        /// <summary>
        /// Causes the specified call to return one of its own arguments.
        /// </summary>
        public static void ReturnsArgumentForAnyArgs<T>(this T instance, int argumentIndex)
        {
            instance.ReturnsForAnyArgs(x => x[argumentIndex]);
        }

        public static void Throws<T>(this T instance, Exception exception)
        {
            instance.Returns(_ => { throw exception; });
        }

        public static void Throws<T>(this T instance, Func<Exception> exceptionFunc)
        {
            instance.Returns(_ => { throw exceptionFunc(); });
        }

        public static void Throws<T>(this T instance, Func<CallInfo, Exception> exceptionFunc)
        {
            instance.Returns(callInfo => { throw exceptionFunc(callInfo); });
        }

        public static void Throws<T>(this T instance, Action<T> substitute, Exception exception) where T : class
        {
            instance.When(substitute).Do(_ => { throw exception; });
        }

        public static void ThrowsForAnyArgs<T>(this T instance, Exception exception)
        {
            instance.ReturnsForAnyArgs(_ => { throw exception; });
        }

        public static void ThrowsForAnyArgs<T>(this T instance, Func<Exception> exceptionFunc)
        {
            instance.ReturnsForAnyArgs(_ => { throw exceptionFunc(); });
        }

        public static void ThrowsForAnyArgs<T>(this T instance, Func<CallInfo, Exception> exceptionFunc)
        {
            instance.ReturnsForAnyArgs(callInfo => { throw exceptionFunc(callInfo); });
        }

        public static void ThrowsForAnyArgs<T>(this T instance, Action<T> substitute, Exception exception) where T : class
        {
            instance.WhenForAnyArgs(substitute).Do(_ => { throw exception; });
        }

        public static void ThrowsTask(this Task instance, Exception exception)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(exception);
            instance.Returns(tcs.Task);
        }

        public static void ThrowsTask<T>(this Task<T> instance, Exception exception)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(exception);
            instance.Returns(tcs.Task);
        }

        public static void ThrowsTaskForAnyArgs(this Task instance, Exception exception)
        {
            var tcs = new TaskCompletionSource<object>();
            tcs.SetException(exception);
            instance.ReturnsForAnyArgs(tcs.Task);
        }

        public static void ThrowsTaskForAnyArgs<T>(this Task<T> instance, Exception exception)
        {
            var tcs = new TaskCompletionSource<T>();
            tcs.SetException(exception);
            instance.ReturnsForAnyArgs(tcs.Task);
        }

        public static void Throws<T>(this object instance)
            where T : Exception, new()
        {
            instance.Returns(_ => { throw new T(); });
        }

        public static void ThrowsForAnyArgs<T>(this object instance)
            where T : Exception, new()
        {
            instance.ReturnsForAnyArgs(_ => { throw new T(); });
        }

        public static WhenCalledAny<T> WhenAny<T>(this T substitute, Action<T> substituteCall) where T : class
        {
            var context = SubstitutionContext.Current;
            return new WhenCalledAny<T>(context, substitute, substituteCall, MatchArgs.Any);
        }

        public static IEnumerable<ICall> WithAnyArgument<TArg>(this IEnumerable<ICall> calls, Func<TArg, bool> predicate)
        {
            return calls.Where(call =>
                call.GetArguments()
                    .OfType<TArg>()
                    .Any(arg => predicate(arg)));
        }

        public class WhenCalledAny<T> : WhenCalled<T>
            where T : class
        {
            public WhenCalledAny(ISubstitutionContext context, T substitute, Action<T> call, MatchArgs matchArgs)
                : base(context, substitute, call, matchArgs) { }

            public void Do<T1>(Action<T1> callbackWithArguments)
            {
                this.Do(callInfo => callbackWithArguments((T1)callInfo[0]));
            }

            public void Do<T1, T2>(Action<T1, T2> callbackWithArguments)
            {
                this.Do(callInfo => callbackWithArguments((T1)callInfo[0], (T2)callInfo[1]));
            }

            public void Do<T1, T2, T3>(Action<T1, T2, T3> callbackWithArguments)
            {
                this.Do(callInfo => callbackWithArguments((T1)callInfo[0], (T2)callInfo[1], (T3)callInfo[2]));
            }

            public void Do<T1, T2, T3, T4>(Action<T1, T2, T3, T4> callbackWithArguments)
            {
                this.Do(callInfo => callbackWithArguments((T1)callInfo[0], (T2)callInfo[1], (T3)callInfo[2], (T4)callInfo[3]));
            }

            public void Do<T1, T2, T3, T4, T5>(Action<T1, T2, T3, T4, T5> callbackWithArguments)
            {
                this.Do(callInfo => callbackWithArguments((T1)callInfo[0], (T2)callInfo[1], (T3)callInfo[2], (T4)callInfo[3], (T5)callInfo[4]));
            }

            public void Do<T1, T2, T3, T4, T5, T6>(Action<T1, T2, T3, T4, T5, T6> callbackWithArguments)
            {
                this.Do(callInfo => callbackWithArguments((T1)callInfo[0], (T2)callInfo[1], (T3)callInfo[2], (T4)callInfo[3], (T5)callInfo[4], (T6)callInfo[5]));
            }
        }
    }
}
