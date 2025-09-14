import { DatePipe } from '@angular/common';
import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'dateTimeAgo',
  pure: false, // Impure pipe to handle real-time updates
})
export class DateTimeAgoPipe implements PipeTransform {
  private cache: { [key: number]: { timestamp: number; value: string } } = {};

  constructor(private datePipe: DatePipe) {}

  transform(relativeTime: number, textOnly: boolean = false): string {
    const now = new Date();
    const currentMinute = Math.floor(now.getTime() / 60000); // Current minute for caching

    // Check cache to avoid recalculating within the same minute
    if (
      this.cache[relativeTime] &&
      this.cache[relativeTime].timestamp === currentMinute
    ) {
      return this.cache[relativeTime].value;
    }

    const utcDate = new Date(
      Date.UTC(
        now.getUTCFullYear(),
        now.getUTCMonth(),
        now.getUTCDate(),
        now.getUTCHours(),
        now.getUTCMinutes(),
        now.getUTCSeconds(),
        now.getUTCMilliseconds()
      )
    );
    const relativeTimeMs = relativeTime * 1000; // Convert seconds to milliseconds
    const diffMs = utcDate.getTime() - relativeTimeMs; // Time difference in milliseconds

    if (diffMs < 0) {
      const result = 'In the future';
      this.cache[relativeTime] = { timestamp: currentMinute, value: result };
      return result;
    }

    const years = Math.floor(diffMs / (1000 * 60 * 60 * 24 * 365.25));
    const remainingAfterYears = diffMs % (1000 * 60 * 60 * 24 * 365.25);
    const days = Math.floor(remainingAfterYears / (1000 * 60 * 60 * 24));
    const remainingAfterDays = remainingAfterYears % (1000 * 60 * 60 * 24);
    const hours = Math.floor(remainingAfterDays / (1000 * 60 * 60));
    const remainingAfterHours = remainingAfterDays % (1000 * 60 * 60);
    const minutes = Math.floor(remainingAfterHours / (1000 * 60));

    let result = '';
    if (years > 0) result += `${years}y`;
    if (days > 0) result += `${days}d`;
    if (hours > 0 || days > 0 || years > 0) result += `${hours}h`;
    result += `${minutes}m ago`;

    const timeAgo = result.trim();

    const value = `<span class="font-bold">${timeAgo}</span> <br /><span class='text-muted'>${this.datePipe.transform(
      new Date(relativeTimeMs),
      'dd/MM/yyyy HH:mm:ss'
    )}</span>`;

    // Cache the result for the current minute
    this.cache[relativeTime] = { timestamp: currentMinute, value };

    if (textOnly) {
      return `${timeAgo} ${this.datePipe.transform(
        new Date(relativeTimeMs),
        'dd/MM/yyyy HH:mm:ss'
      )}`;
    }
    return value;
  }
}
