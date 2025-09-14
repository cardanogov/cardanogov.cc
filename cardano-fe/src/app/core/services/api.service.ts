import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { ADAStatic } from '../../shared/models/voting.model';
import { environment } from '../../../environments/environment';

@Injectable({ providedIn: 'root' })
export class ApiService {
  private apiUrl = environment.apiUrl;
  //private apiUrl = `${environment.apiUrl}/api/v1`;
  private readonly headers = new HttpHeaders({
    Accept: 'application/json',
    'Content-Type': 'application/json',
  });

  constructor(private http: HttpClient) {}

  get<T>(
    endpoint: string,
    params?:
      | HttpParams
      | Record<
          string,
          string | number | boolean | ReadonlyArray<string | number | boolean>
        >
  ): Observable<T> {
    return this.http.get<T>(`${this.apiUrl}/${endpoint}`, {
      headers: this.headers,
      params: params,
    });
  }

  post<T>(endpoint: string, data: any): Observable<T> {
    return this.http.post<T>(`${this.apiUrl}/${endpoint}`, data, {
      headers: this.headers,
    });
  }
}
