using System;

namespace ModelessForms.IssuesManager.Storage
{
    /// <summary>
    /// A storage operation failed. The message is written for the user and names
    /// the file or folder. The original exception, if any, is the inner exception.
    /// </summary>
    public sealed class IssueStoreException : Exception
    {
        public IssueStoreException(string message) : base(message) { }

        public IssueStoreException(string message, Exception inner) : base(message, inner) { }
    }
}
