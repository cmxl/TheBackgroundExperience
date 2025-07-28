namespace TheBackgroundExperience.Domain.Exceptions;

public class InvalidZipCodeException : Exception
{
	public string ZipCode { get; }

	public InvalidZipCodeException(string zipCode, string message)
		: base(message)
	{
		ZipCode = zipCode;
	}

	public static void ThrowIfInvalidFormat(string zipCode)
	{
		if(string.IsNullOrWhiteSpace(zipCode))
			throw new InvalidZipCodeException(zipCode, "Zip code cannot be null or empty.");
		
		if(zipCode.Length != 5)
			throw new InvalidZipCodeException(zipCode, "Zip code must be exactly 5 characters long.");
	}
}