namespace SkyRoute.BusinessLogic.Documents;

public enum DocumentKind
{
    NationalId = 1,
    Passport = 2
}

public interface IDocumentValidator
{
    DocumentKind Kind { get; }

    bool IsValid(string documentNumber);
}

public sealed class NationalIdValidator : IDocumentValidator
{
    public DocumentKind Kind => DocumentKind.NationalId;

    public bool IsValid(string documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            return false;
        }

        var trimmed = documentNumber.Trim();
        if (trimmed.Length is < 5 or > 12)
        {
            return false;
        }

        foreach (var c in trimmed)
        {
            if (!char.IsDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}

public sealed class PassportValidator : IDocumentValidator
{
    public DocumentKind Kind => DocumentKind.Passport;

    public bool IsValid(string documentNumber)
    {
        if (string.IsNullOrWhiteSpace(documentNumber))
        {
            return false;
        }

        var trimmed = documentNumber.Trim().ToUpperInvariant();
        if (trimmed.Length is < 6 or > 9)
        {
            return false;
        }

        foreach (var c in trimmed)
        {
            if (!char.IsLetterOrDigit(c))
            {
                return false;
            }
        }

        return true;
    }
}

public interface IDocumentValidatorFactory
{
    IDocumentValidator For(bool isInternational);
}

public sealed class DocumentValidatorFactory : IDocumentValidatorFactory
{
    public IDocumentValidator For(bool isInternational) =>
        isInternational ? new PassportValidator() : new NationalIdValidator();
}
