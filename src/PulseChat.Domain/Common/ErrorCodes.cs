namespace PulseChat.Domain.Common;

public static class ErrorCodes
{
    public const string ValidationFailed = "VALIDATION_FAILED";
    public const string UserNotFound = "USER_NOT_FOUND";
    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string EmailAlreadyExists = "EMAIL_ALREADY_EXISTS";
    public const string UsernameAlreadyExists = "USERNAME_ALREADY_EXISTS";
    public const string InvalidToken = "INVALID_TOKEN";
    public const string TokenExpired = "TOKEN_EXPIRED";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string RoomNotFound = "ROOM_NOT_FOUND";
    public const string NotRoomMember = "NOT_ROOM_MEMBER";
    public const string DuplicateMessage = "DUPLICATE_MESSAGE";
    public const string FriendshipAlreadyExists = "FRIENDSHIP_ALREADY_EXISTS";
    public const string FriendshipNotFound = "FRIENDSHIP_NOT_FOUND";
    public const string CannotAddSelfAsFriend = "CANNOT_ADD_SELF_AS_FRIEND";
    public const string FileUploadFailed = "FILE_UPLOAD_FAILED";
    public const string InternalServerError = "INTERNAL_SERVER_ERROR";
}
