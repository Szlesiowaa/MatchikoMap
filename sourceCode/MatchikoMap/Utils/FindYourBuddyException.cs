namespace MatchikoMap.Utils
{
    public class MatchikoMapException(string message) : Exception(message)
    {
    }

    public class MatchmakingNotFoundException : MatchikoMapException
    {
        public MatchmakingNotFoundException() : base("Nie znaleziono matchmakingu") { }
    }
    public class MatchmakingNotReadyException : MatchikoMapException
    {
        public MatchmakingNotReadyException():base("W matchmakingu bierze udział tylko jedna osoba") { }
    }
    public class AlreadyFriendsException(int conversationId, int friendId) : MatchikoMapException("Jesteście już znajomymi")
    {
        public int ConversationId { get; } = conversationId;
        public int FriendId { get; } = friendId;
    }
    public class MatchmakingJoiningFailedException : MatchikoMapException
    {
        public MatchmakingJoiningFailedException() : base("Nie udało się dołączyć do matchmakingu") { }
    }
    public class FileTooLargeException : MatchikoMapException
    {
        public FileTooLargeException() : base("Plik jest zbyt duży") { }
    }
}
