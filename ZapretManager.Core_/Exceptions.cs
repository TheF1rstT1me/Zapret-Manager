namespace ZapretManager.Core_.Exceptions;

public class GitHubServiceException : Exception
{
    public GitHubServiceException(string message) : base(message) { }
    public GitHubServiceException(string message, Exception inner) : base(message, inner) { }
}

public class SourceForgeException : Exception
{
    public SourceForgeException(string message) : base(message) { }
    public SourceForgeException(string message, Exception inner) : base(message, inner) { }
}