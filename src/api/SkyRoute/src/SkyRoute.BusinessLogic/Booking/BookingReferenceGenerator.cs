namespace SkyRoute.BusinessLogic.Booking;

using System.Security.Cryptography;

public interface IBookingReferenceGenerator
{
    string Generate();
}

public sealed class BookingReferenceGenerator : IBookingReferenceGenerator
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public string Generate()
    {
        Span<char> buffer = stackalloc char[6];
        for (var i = 0; i < buffer.Length; i++)
        {
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
        }

        return string.Concat("SR-", new string(buffer));
    }
}
