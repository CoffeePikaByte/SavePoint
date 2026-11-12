namespace GameHub.Application.Exceptions;

public class LibraryGameNotFoundException(Exception? innerException = null)
    : Exception("Game not found in the user's library.", innerException);
