import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  SectionInfo, CurriculumUnitInfo, GenerateExerciseRequest,
  ExerciseResponse, SubmitExerciseRequest, ExerciseResultResponse,
  ExerciseHistoryItem, PaginatedResponse
} from '../../shared/models/api.models';

@Injectable({ providedIn: 'root' })
export class ExerciseService {
  private readonly API_URL = 'http://localhost:5000/api/exercise';

  constructor(private http: HttpClient) {}

  getSections(): Observable<SectionInfo[]> {
    return this.http.get<SectionInfo[]>(`${this.API_URL}/sections`);
  }

  getUnits(): Observable<CurriculumUnitInfo[]> {
    return this.http.get<CurriculumUnitInfo[]>(`${this.API_URL}/units`);
  }

  generate(request: GenerateExerciseRequest): Observable<ExerciseResponse> {
    return this.http.post<ExerciseResponse>(`${this.API_URL}/generate`, request);
  }

  getExercise(id: number): Observable<ExerciseResponse> {
    return this.http.get<ExerciseResponse>(`${this.API_URL}/${id}`);
  }

  submit(id: number, request: SubmitExerciseRequest): Observable<ExerciseResultResponse> {
    return this.http.post<ExerciseResultResponse>(`${this.API_URL}/${id}/submit`, request);
  }

  getHistory(page = 1, pageSize = 20): Observable<PaginatedResponse<ExerciseHistoryItem>> {
    return this.http.get<PaginatedResponse<ExerciseHistoryItem>>(
      `${this.API_URL}/history?page=${page}&pageSize=${pageSize}`
    );
  }
}
