import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  SectionInfo, CurriculumUnitInfo, GenerateExerciseRequest,
  ExerciseResponse, SubmitExerciseRequest, ExerciseResultResponse,
  ExerciseHistoryItem, PaginatedResponse, RetakeMistakesRequest
} from '../../shared/models/api.models';

@Injectable({ providedIn: 'root' })
export class ExerciseService {
  private readonly API_URL = 'http://localhost:5100/api/exercise';

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

  generateStream(request: GenerateExerciseRequest): Observable<{ type: string, text?: string, exercise?: ExerciseResponse, message?: string }> {
    return new Observable(observer => {
      const token = localStorage.getItem('token');
      
      fetch(`${this.API_URL}/generate-stream`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          'Authorization': token ? `Bearer ${token}` : ''
        },
        body: JSON.stringify(request)
      }).then(async response => {
        if (!response.body) {
          observer.error('No response body');
          return;
        }
        
        const reader = response.body.getReader();
        const decoder = new TextDecoder('utf-8');
        let buffer = '';

        while (true) {
          const { done, value } = await reader.read();
          if (done) break;
          
          buffer += decoder.decode(value, { stream: true });
          
          const lines = buffer.split('\n\n');
          buffer = lines.pop() || '';
          
          for (const line of lines) {
            if (line.startsWith('data: ')) {
              const dataStr = line.substring(6);
              try {
                const data = JSON.parse(dataStr);
                observer.next(data);
                if (data.type === 'done' || data.type === 'error') {
                  observer.complete();
                  return;
                }
              } catch (e) {
                // Ignore parse errors
              }
            }
          }
        }
        
        if (buffer.startsWith('data: ')) {
          try {
             const data = JSON.parse(buffer.substring(6));
             observer.next(data);
          } catch(e) {}
        }
        
        observer.complete();
      }).catch(err => {
        observer.error(err);
      });
    });
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

  getReview(id: number): Observable<ExerciseResultResponse> {
    return this.http.get<ExerciseResultResponse>(`${this.API_URL}/${id}/review`);
  }

  reAnswerExercise(id: number): Observable<{ newExerciseId: number }> {
    return this.http.post<{ newExerciseId: number }>(`${this.API_URL}/${id}/re-answer`, {});
  }

  retakeMistakes(request: RetakeMistakesRequest): Observable<{ newExerciseId: number }> {
    return this.http.post<{ newExerciseId: number }>(`${this.API_URL}/retake-mistakes`, request);
  }
}
