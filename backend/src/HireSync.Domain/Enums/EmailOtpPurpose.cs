namespace HireSync.Domain.Enums;

public enum EmailOtpPurpose : byte
{
    EmployerRegistration = 1,
    AdministratorFirstActivation = 2,
    JobSeekerRegistration = 3,
    PasswordReset = 4
}
