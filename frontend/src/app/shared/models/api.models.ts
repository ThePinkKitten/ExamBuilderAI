// ===== Auth Models =====
export interface LoginRequest {
  username: string;
  password: string;
}

export interface RegisterRequest {
  username: string;
  password: string;
  displayName: string;
}

export interface AuthResponse {
  token: string;
  user: UserInfo;
}

export interface UserInfo {
  id: number;
  username: string;
  displayName: string;
}

// ===== Exercise Models =====
export interface SectionInfo {
  id: number;
  code: string;
  name: string;
  description: string;
  icon: string;
  totalExercises: number;
  averageScore: number | null;
}

export interface CurriculumUnitInfo {
  id: number;
  unitNumber: number;
  unitTitle: string;
}

export interface GenerateExerciseRequest {
  sectionCode: string;
  curriculumUnitId?: number | null;
  questionCount: number;
}

export interface RetakeMistakesRequest {
  sectionCode?: string;
  questionCount?: number;
}

export interface ExerciseResponse {
  id: number;
  sectionCode: string;
  sectionName: string;
  unitTitle: string | null;
  questionCount: number;
  questions: any;
  createdAt: string;
  hasBeenSubmitted: boolean;
}

export interface SubmitExerciseRequest {
  userAnswers: { [key: string]: any };
  timeTakenSeconds: number;
}

export interface ExerciseResultResponse {
  exerciseId: number;
  sectionCode: string;
  sectionName: string;
  correctCount: number;
  totalQuestions: number;
  scorePercent: number;
  timeTakenSeconds: number;
  fullContent: any;
  userAnswers: any;
  aiFeedback: string | null;
  completedAt: string;
}

export interface ExerciseHistoryItem {
  exerciseId: number;
  sectionCode: string;
  sectionName: string;
  unitTitle: string | null;
  scorePercent: number | null;
  createdAt: string;
  completedAt: string | null;
}

export interface PaginatedResponse<T> {
  items: T[];
  total: number;
  page: number;
  pageSize: number;
}

// ===== Progress Models =====
export interface ProgressOverview {
  totalExercisesDone: number;
  overallAverageScore: number;
  totalQuestionsAnswered: number;
  totalCorrectAnswers: number;
  sectionStats: SectionProgress[];
}

export interface SectionProgress {
  sectionCode: string;
  sectionName: string;
  exercisesDone: number;
  averageScore: number;
  totalQuestions: number;
  correctAnswers: number;
}
