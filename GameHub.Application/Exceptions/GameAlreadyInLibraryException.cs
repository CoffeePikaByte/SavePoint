namespace GameHub.Application.Exceptions;

public class GameAlreadyInLibraryException(Exception? innerException = null)
    : Exception("Game is already in the user's library.", innerException);
