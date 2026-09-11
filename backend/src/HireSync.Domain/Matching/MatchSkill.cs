using HireSync.Domain.Enums;

namespace HireSync.Domain.Matching;

public sealed record MatchSkill
{
    public Guid Id { get; }

    public string Name { get; }

    public string NormalizedName { get; }

    public MatchSkill(
        Guid id,
        string name,
        string normalizedName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException(
                "Skill ID cannot be empty.",
                nameof(id));
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Skill name is required.",
                nameof(name));
        }

        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            throw new ArgumentException(
                "Normalized skill name is required.",
                nameof(normalizedName));
        }

        Id = id;
        Name = name;
        NormalizedName = normalizedName;
    }
}
