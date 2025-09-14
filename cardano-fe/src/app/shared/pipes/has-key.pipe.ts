import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'hasKey'
})
export class HasKeyPipe implements PipeTransform {
  transform(value: string, key: string): boolean {
    if (!value) return false;
    try {
      const obj = JSON.parse(value);
      return obj && key in obj;
    } catch {
      return false;
    }
  }
}
