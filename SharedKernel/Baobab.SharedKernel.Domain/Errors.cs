using Baobab.SharedKernel.Domain.Results;
using Baobab.SharedKernel.Domain.ValueObjects;

namespace Baobab.SharedKernel.Domain;

internal sealed record Errors
{
    internal sealed record UserErrors
    {
        internal static Error UserIdEmpty => new("user_id_empty", "User id cannot be empty");
    }

    internal sealed record FirstNameErrors
    {
        internal static Error FirstNameNullOrEmpty => new("first_name_null_or_empty", "First name cannot be null or empty");
        internal static Error FirstNameTooLong => new("first_name_too_long", $"First name cannot be longer than {FirstName.MaxLength} characters");
    }

    internal sealed record LastNameErrors
    {
        internal static Error LastNameNullOrEmpty => new("last_name_null_or_empty", "Last name cannot be null or empty");
        internal static Error LastNameTooLong => new("last_name_too_long", $"Last name cannot be longer than {LastName.MaxLength} characters");
    }

    internal sealed record OtherNameErrors
    {
        internal static Error OtherNameTooLong => new("other_name_too_long", $"Other name cannot be longer than {OtherName.MaxLength} characters");
    }

    internal sealed record EmailAddressErrors
    {
        internal static Error EmailAddressNullOrEmpty => new("email_address_null_or_empty", "Email address cannot be null or empty");
        internal static Error EmailAddressTooLong => new("email_address_too_long", $"Email address cannot be longer than {EmailAddress.MaxLength} characters");
        internal static Error EmailAddressInvalid => new("email_address_invalid", "Email address is invalid");
        internal static Error EmailAddressInvalidFormat => new("email_address_invalid_format", "Email address is invalid format");
        internal static Error EmailAddressInvalidDomain => new("email_address_invalid_domain", "Email address is invalid domain");
        internal static Error EmailAddressInvalidLocalPart => new("email_address_invalid_local_part", "Email address is invalid local part");
        internal static Error EmailAddressTooShortOrTooLong => new("email_address_too_short_or_too_long",
            $"Email address is too short or too long. Must be between {EmailAddress.MinLength} and {EmailAddress.MinLength}");
    }

    internal sealed record PhoneNumberErrors
    {
        internal static Error PhoneNumberNullOrEmpty => new("phone_number_null_or_empty", "Phone number cannot be null or empty");
        internal static Error PhoneNumberTooLong => new("phone_number_too_long", $"Phone number cannot be longer than {PhoneNumber.MaxLength} characters");
        internal static Error PhoneNumberInvalid => new("phone_number_invalid", "Phone number is invalid");
        internal static Error PhoneNumberInvalidFormat => new("phone_number_invalid_format", "Phone number is invalid format");
        internal static Error PhoneNumberInvalidCountryCode => new("phone_number_invalid_country_code", "Phone number is invalid country code");
        internal static Error PhoneNumberInvalidNumber => new("phone_number_invalid_number", "Phone number is invalid number");
        internal static Error PhoneNumberTooShortOrTooLong => new("phone_number_too_short_or_too_long",
                       $"Phone number is too short or too long. Must be between {PhoneNumber.MinLength} and {PhoneNumber.MinLength}");
        internal static Error PhoneNumberCountryCodeNullOrEmpty => new("phone_number_country_code_null_or_empty", "Phone number country code cannot be null or empty");
    }

    internal sealed record GhanaCardPersonalIdentificationNumberErrors
    {
        internal static Error GhanaCardPersonalIdentificationNumberNullOrEmpty => new("ghana_card_personal_identification_number_null_or_empty", "Ghana card personal identification number cannot be null or empty");
        internal static Error GhanaCardPersonalIdentificationNumberInvalid => new("ghana_card_personal_identification_number_invalid", "Ghana card personal identification number is invalid");
        internal static Error GhanaCardPersonalIdentificationNumberInvalidFormat => new("ghana_card_personal_identification_number_invalid_format", "Ghana card personal identification number is invalid format");
        internal static Error GhanaCardPersonalIdentificationNumberTooShortOrTooLong => new("ghana_card_personal_identification_number_too_short_or_too_long",
                       $"Ghana card personal identification number is too short or too long. Must be {GhanaCardPersonalIdentificationNumber.Length} long.");
    }

    internal sealed record MoneyErrors
    {
        public static Error CurrencyMismatch => new("currency_mismatch", "Currency mismatch");

        internal static Error CurrencyNullOrEmpty => new("currency_null_or_empty", "Currency cannot be null or empty");
        internal static Error InvalidAmount => new("amount_negative", "Invalid amount. Amount cannot be zero nor negative");
    }

    internal sealed record DateOfBirth
    {
        internal static Error DateOfBirthNullOrEmpty => new("date_of_birth_null_or_empty", "Date of birth cannot be null or empty");
        internal static Error DateOfBirthInFuture => new("date_of_birth_in_future", "Date of birth cannot be in the future");
        internal static Error DateOfBirthLessThan18Years => new("date_of_birth_less_than_18_years", "Date of birth cannot be less than 18 years");
        internal static Error DateOfBirthInvalidFormat => new("date_of_birth_invalid_format", "Date of birth is invalid format");
    }
}
