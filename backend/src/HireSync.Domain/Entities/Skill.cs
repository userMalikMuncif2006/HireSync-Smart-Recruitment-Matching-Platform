using HireSync.Domain.Rules;

namespace HireSync.Domain.Entities;

public sealed class Skill
{
    public Guid Id { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    private Skill()
    {
    }

    public Skill(Guid id, string name)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Skill ID cannot be empty.",
                nameof(id));
        }

        Id = id;
        Name = SkillNameNormalizer.CanonicalizeDisplayName(name);
        NormalizedName = SkillNameNormalizer.Normalize(Name);
    }
}
