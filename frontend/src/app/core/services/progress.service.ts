import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { ProgressOverview } from '../../shared/models/api.models';

@Injectable({ providedIn: 'root' })
export class ProgressService {
  private readonly API_URL = 'http://localhost:5000/api/progress';

  constructor(private http: HttpClient) {}

  getOverview(): Observable<ProgressOverview> {
    return this.http.get<ProgressOverview>(`${this.API_URL}/overview`);
  }
}
