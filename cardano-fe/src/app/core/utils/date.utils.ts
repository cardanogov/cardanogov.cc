/**
 * Gets the number of minutes remaining until the end of the current day
 * @returns number of minutes until end of day
 */
export function getMinutesUntilEndOfDay(): number {
  const now = new Date();

  // Create the UTC end of the day (23:59:59.999 UTC)
  const endOfDay = new Date(Date.UTC(
    now.getUTCFullYear(),
    now.getUTCMonth(),
    now.getUTCDate(),
    23, 59, 59, 999
  ));

  // Calculate the difference in milliseconds
  const diffMs = endOfDay.getTime() - now.getTime();

  // Convert milliseconds to minutes and round up
  return Math.ceil(diffMs / (1000 * 60));
}


/**
 * Gets the number of minutes until the next occurrence of a specific hour (in 24h format)
 * @param hour Hour in 24h format (0-23)
 * @returns number of minutes until the next occurrence of the specified hour
 */
export function getMinutesUntilHour(hour: number): number {
  if (hour < 0 || hour > 23) {
    throw new Error('Hour must be between 0 and 23');
  }

  const now = new Date();
  const target = new Date(
    now.getFullYear(),
    now.getMonth(),
    now.getDate(),
    hour,
    0,
    0,
    0
  );

  // If the target hour has already passed today, add 24 hours
  if (target.getTime() <= now.getTime()) {
    target.setDate(target.getDate() + 1);
  }

  const diffMs = target.getTime() - now.getTime();
  return Math.ceil(diffMs / (1000 * 60)); // Convert ms to minutes and round up
}

/**
 * Format minutes into a human readable duration string
 * @param minutes Number of minutes
 * @returns Formatted string (e.g. "2 hours 30 minutes" or "45 minutes")
 */
export function formatMinutesDuration(minutes: number): string {
  if (minutes < 0) {
    return '0 minutes';
  }

  const hours = Math.floor(minutes / 60);
  const remainingMinutes = minutes % 60;

  if (hours === 0) {
    return `${remainingMinutes} minute${remainingMinutes !== 1 ? 's' : ''}`;
  }

  if (remainingMinutes === 0) {
    return `${hours} hour${hours !== 1 ? 's' : ''}`;
  }

  return `${hours} hour${hours !== 1 ? 's' : ''} ${remainingMinutes} minute${
    remainingMinutes !== 1 ? 's' : ''
  }`;
}

/**
 * Convert a Unix timestamp (in seconds) to a Date object
 * @param timestamp Unix timestamp in seconds
 * @returns Date object
 */
export function timestampToDate(timestamp: number): Date {
  // Multiply by 1000 to convert seconds to milliseconds
  return new Date(timestamp * 1000);
}

/**
 * Convert a Date object to a Unix timestamp (in seconds)
 * @param date Date object or string that can be parsed by Date constructor
 * @returns Unix timestamp in seconds
 */
export function dateToTimestamp(date: Date | string): number {
  const dateObj = typeof date === 'string' ? new Date(date) : date;
  // Divide by 1000 to convert milliseconds to seconds
  return Math.floor(dateObj.getTime() / 1000);
}

/**
 * Format a date or timestamp to a human-readable string
 * @param input Date object, timestamp (in seconds), or date string
 * @param options Intl.DateTimeFormatOptions for customizing the output
 * @returns Formatted date string
 */
export function formatDate(
  input: Date | number | string,
  options: Intl.DateTimeFormatOptions = {
    year: 'numeric',
    month: 'short',
    day: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }
): string {
  let date: Date;

  if (typeof input === 'number') {
    date = timestampToDate(input);
  } else if (typeof input === 'string') {
    date = new Date(input);
  } else {
    date = input;
  }

  return new Intl.DateTimeFormat('en-US', options).format(date);
}

/**
 * Check if a timestamp or date is within a specified time range
 * @param date Date to check (Date object, timestamp in seconds, or date string)
 * @param startDate Start of range (Date object, timestamp in seconds, or date string)
 * @param endDate End of range (Date object, timestamp in seconds, or date string)
 * @returns boolean indicating if date is within range
 */
export function isDateInRange(
  date: Date | number | string,
  startDate: Date | number | string,
  endDate: Date | number | string
): boolean {
  const convertToDate = (input: Date | number | string): Date => {
    if (typeof input === 'number') {
      return timestampToDate(input);
    }
    if (typeof input === 'string') {
      return new Date(input);
    }
    return input;
  };

  const checkDate = convertToDate(date);
  const start = convertToDate(startDate);
  const end = convertToDate(endDate);

  return checkDate >= start && checkDate <= end;
}
