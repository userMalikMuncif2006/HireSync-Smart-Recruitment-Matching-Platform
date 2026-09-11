namespace HireSync.Domain.Matching;

public interface IMatchEngine
{
    MatchResult Calculate(
        CandidateMatchInput candidate,
        VacancyMatchInput vacancy);
}
