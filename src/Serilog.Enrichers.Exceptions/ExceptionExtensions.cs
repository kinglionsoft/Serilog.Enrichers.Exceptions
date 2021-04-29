using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.ExceptionServices;

namespace Serilog.Enrichers.Exceptions
{
    /// <summary>
    /// From https://github.com/aelij/AsyncFriendlyStackTrace
    /// </summary>
    public static class ExceptionExtensions
    {
        private const string EndOfInnerExceptionStack = "--- End of inner exception stack trace ---";
        private const string AggregateExceptionFormatString = "{0}{1}---> (Inner Exception #{2}) {3}{4}{5}";
        private const string AsyncStackTraceExceptionData = "AsyncFriendlyStackTrace";
        private const string NewLine = " ---> ";

        public static void ReThrow(this Exception exception)
        {
            ExceptionDispatchInfo.Capture(exception).Throw();
        }

        public static string ToFriendlyMessage(this Exception exception)
        {
            if (exception == null!) return string.Empty;

            var innerExceptions = GetInnerExceptions(exception);
            var message = innerExceptions != null
                ? ToAsyncAggregateString(exception, innerExceptions)
                : ToAsyncStringCore(exception, includeMessageOnly: false);
            return message;
        }

        /// <summary>
        /// Prepares an <see cref="Exception"/> for serialization by including the async-friendly
        /// stack trace as additional <see cref="Exception.Data"/>.
        /// Note that both the original and the new stack traces will be serialized.
        /// This method operates recursively on all inner exceptions,
        /// including ones in an <see cref="AggregateException"/>.
        /// </summary>
        /// <param name="exception">The exception.</param>
        public static void PrepareForAsyncSerialization(this Exception exception)
        {
            if (exception == null) throw new ArgumentNullException(nameof(exception));

            if (exception.Data[AsyncStackTraceExceptionData] != null || exception.StackTrace != null) return;

            exception.Data[AsyncStackTraceExceptionData] = GetAsyncStackTrace(exception);

            var innerExceptions = GetInnerExceptions(exception);
            if (innerExceptions != null)
            {
                foreach (var innerException in innerExceptions)
                {
                    innerException.PrepareForAsyncSerialization();
                }
            }
            else
            {
                exception.InnerException?.PrepareForAsyncSerialization();
            }
        }

        private static IList<Exception> GetInnerExceptions(Exception exception)
        {
            if (exception is AggregateException aggregateException)
            {
                return aggregateException.InnerExceptions;
            }

            if (exception is ReflectionTypeLoadException reflectionTypeLoadException)
            {
                return reflectionTypeLoadException.LoaderExceptions!;
            }

            return null;
        }

        private static string ToAsyncAggregateString(Exception exception, IList<Exception> inner)
        {
            var s = ToAsyncStringCore(exception, includeMessageOnly: true);
            for (var i = 0; i < inner.Count; i++)
            {
                s = string.Format(CultureInfo.InvariantCulture, AggregateExceptionFormatString, s,
                    NewLine, i, inner[i].ToFriendlyMessage(), "<---", NewLine);
            }
            return s;
        }

        private static string ToAsyncStringCore(Exception exception, bool includeMessageOnly)
        {
            var message = exception.Message.Replace(Environment.NewLine, " ");
            var className = exception.GetType().ToString();
            var s = message.Length <= 0 ? className : className + ": " + message;

            var innerException = exception.InnerException;
            if (innerException != null)
            {
                if (includeMessageOnly)
                {
                    do
                    {
                        s += " ---> " + innerException.Message;
                        innerException = innerException.InnerException;
                    } while (innerException != null);
                }
                else
                {
                    s += " ---> " + innerException.ToFriendlyMessage() + NewLine +
                         "   " + EndOfInnerExceptionStack;
                }
            }

            var trace = GetAsyncStackTrace(exception);
            if (!string.IsNullOrEmpty(trace))
            {
                s += NewLine + trace;
            }

            return s;
        }

        private static string GetAsyncStackTrace(System.Exception exception)
        {
            if (exception.Data[AsyncStackTraceExceptionData] is string stackTrace)
            {
                return stackTrace;
            }

            stackTrace = exception.StackTrace;
            if (!string.IsNullOrEmpty(stackTrace))
            {
                return stackTrace.Replace(System.Environment.NewLine, NewLine);
            }

            stackTrace = new StackTrace(exception, true).ToAsyncString();
            return stackTrace;
        }
    }
}