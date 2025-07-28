using TheBackgroundExperience.Domain.Exceptions;

namespace TheBackgroundExperience.Domain.ValueObjects;

public class ZipCode : ValueObject
{
	private const string DefaultCode = "00000";
	
	private ZipCode()
	{
	}

	private ZipCode(string code)
	{
		Code = code;
	}

	public string Code { get; private set; } = DefaultCode;

	public static ZipCode From(string code)
	{
		InvalidZipCodeException.ThrowIfInvalidFormat(code);
		return new ZipCode { Code = code };
	}

	public static ZipCode Undefined => new(DefaultCode);
	
	protected override IEnumerable<object> GetEqualityComponents()
	{
		yield return Code;
	}

	public override string ToString()
	{
		return Code;
	}
	
	public static explicit operator ZipCode(string code)
	{
		return From(code);
	}
	
	public static implicit operator string(ZipCode zipCode)
	{
		return zipCode.ToString();
	}
}