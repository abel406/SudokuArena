namespace SudokuArena.Application.Contracts.Theming;

public sealed record UploadMediaRequest(
    string FileName,
    string ContentType,
    Stream Content,
    string PublicUrlBase);
