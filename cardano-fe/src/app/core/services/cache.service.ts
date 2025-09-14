import { Injectable } from '@angular/core';
import { Observable, of } from 'rxjs';
import { shareReplay, tap } from 'rxjs/operators';

/**
 * Lớp trừu tượng cung cấp một giải pháp caching chung, có thể tái sử dụng.
 * Cache tồn tại vĩnh viễn trong suốt phiên làm việc và chỉ bị xóa khi
 * người dùng tải lại trang hoặc xóa thủ công.
 */
@Injectable()
export abstract class CacheService {
  // Map để lưu trữ các observables đã được cache.
  // Key: một chuỗi định danh duy nhất cho mỗi request (ví dụ: 'users', 'products-1').
  // Value: Observable đã được cache.
  private cache = new Map<string, Observable<any>>();

  /**
   * Phương thức được bảo vệ để lấy dữ liệu từ cache hoặc từ API nếu chưa có.
   * @param key Khóa định danh cho cache.
   * @param source$ Observable nguồn (ví dụ: một http.get() call).
   * @returns Observable chứa dữ liệu đã được cache hoặc dữ liệu mới từ API.
   */
  protected getCachedData<T>(key: string, source$: Observable<T>): Observable<T> {
    // Kiểm tra xem observable đã tồn tại trong cache chưa.
    const cachedObservable = this.cache.get(key);
    if (cachedObservable) {
      return cachedObservable;
    }

    // Nếu chưa có trong cache, tạo một observable mới.
    const newObservable = source$.pipe(
      tap(),
      // shareReplay(1) đảm bảo API chỉ được gọi một lần và kết quả cuối cùng
      // sẽ được lưu lại và chia sẻ cho tất cả các subscriber sau này.
      shareReplay(1)
    );

    // Lưu observable mới vào cache.
    this.cache.set(key, newObservable);
    return newObservable;
  }

  /**
   * Xóa một mục cache cụ thể hoặc toàn bộ cache.
   * @param key (Tùy chọn) Khóa của cache cần xóa. Nếu để trống, toàn bộ cache sẽ bị xóa.
   */
  clearCache(key?: string): void {
    if (key) {
      if (this.cache.has(key)) {
        this.cache.delete(key);
      }
    } else {
      this.cache.clear();
    }
  }

  /**
   * Kiểm tra xem một key có tồn tại trong cache không.
   * @param key Khóa cần kiểm tra.
   * @returns boolean
   */
  hasCache(key: string): boolean {
    return this.cache.has(key);
  }
}
