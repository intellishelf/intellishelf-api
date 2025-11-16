export interface ApiError {
  title: string;
  status: number;
  type?: string;
  detail?: string;
}

export interface ProblemDetails {
  title: string;
  status: number;
  type: string;
}
