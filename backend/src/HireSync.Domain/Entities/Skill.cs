namespace HireSync.Domain.Entities;

public sealed class Skill
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    private Skill()
    {
    }

    public Skill(Guid id, string name, string normalizedName)
    {
        Id = id;
        Name = name;
        NormalizedName = normalizedName;
    }
}