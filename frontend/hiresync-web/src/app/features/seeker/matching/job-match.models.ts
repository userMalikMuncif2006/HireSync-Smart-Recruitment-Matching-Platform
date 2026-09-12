export interface MatchSkill {
  id: string;
  name: string;
}

export interface MatchResult {
  totalScore: number;
  skillsScore: number;
  experienceScore: number;
  educationScore: number;
  locationScore: number;
  matchedSkills: MatchSkill[];
  missingSkills: MatchSkill[];
}
