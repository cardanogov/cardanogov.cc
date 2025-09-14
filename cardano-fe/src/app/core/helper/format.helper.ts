export const QUADRILLION = 1e15;
export const TRILLION = 1e12;
export const BILLION = 1e9;
export const MILLION = 1e6;
export const THOUSAND = 1e3;

/**
 * Formats a numeric value into human-readable units (K, M, B, T, Q).
 * @param value The numeric value to format.
 * @returns A formatted string with the appropriate unit.
 */
export function formatValue(value: number | string | undefined, fixed: number = 2): string {
  if (!value) return '0';
  if (typeof value === 'string') {
    value = parseFloat(value);
  }
  if (isNaN(value)) return '0';
  if (value >= QUADRILLION) return `${(value / QUADRILLION).toFixed(fixed)}Q`; // Quadrillion
  if (value >= TRILLION) return `${(value / TRILLION).toFixed(fixed)}T`; // Trillion
  if (value >= BILLION) return `${(value / BILLION).toFixed(fixed)}B`; // Billion
  if (value >= MILLION) return `${(value / MILLION).toFixed(fixed)}M`; // Million
  if (value >= THOUSAND) return `${(value / THOUSAND).toFixed(fixed)}K`; // Thousand
  return value.toFixed(fixed); // Raw value
}

/**
 * Rounds a number to two decimal places.
 * @param value The number to round.
 * @returns The rounded number.
 */
export function roundToTwoDecimals(value: number): number {
  return parseFloat(value.toFixed(2));
}

/**
 * Calculates the percentage change between two numbers.
 * @param previous The previous value.
 * @param current The current value.
 * @returns The percentage change.
 */
export function calculateChange(previous: number, current: number): number {
  if (!previous || !current) return 0;
  return ((current - previous) / previous) * 100;
}

export function formatToTrillion(value: string | number, fixed: number = 1): string {
  return (Number(value) / 1e9).toFixed(fixed);
}

export function  formatType(type: string): string {
  // Add space before capital letters
  return type.replace(/([A-Z])/g, ' $1').trim();
}

export function formatRepresentative(representative: string): string {
  if (!representative) return '';
  if (representative.length <= 15) return representative;
  return `${representative.substring(0, 12)}...`;
}

export function formatBlockTime(timestamp: string): string {
  const date = new Date(parseInt(timestamp) * 1000);
  const now = new Date();
  const diffInSeconds = Math.floor((now.getTime() - date.getTime()) / 1000);

  // Less than a minute
  if (diffInSeconds < 60) {
    return 'just now';
  }

  // Less than an hour
  if (diffInSeconds < 3600) {
    const minutes = Math.floor(diffInSeconds / 60);
    return `${minutes} ${minutes === 1 ? 'minute' : 'minutes'} ago`;
  }

  // Less than a day
  if (diffInSeconds < 86400) {
    const hours = Math.floor(diffInSeconds / 3600);
    return `${hours} ${hours === 1 ? 'hour' : 'hours'} ago`;
  }

  // Less than a month
  if (diffInSeconds < 2592000) {
    const days = Math.floor(diffInSeconds / 86400);
    return `${days} ${days === 1 ? 'day' : 'days'} ago`;
  }

  // Less than a year
  if (diffInSeconds < 31536000) {
    const months = Math.floor(diffInSeconds / 2592000);
    return `${months} ${months === 1 ? 'month' : 'months'} ago`;
  }

  // More than a year
  const years = Math.floor(diffInSeconds / 31536000);
  return `${years} ${years === 1 ? 'year' : 'years'} ago`;
}

/**
 * Divides a value by a divisor and formats the result to 1 decimal place
 * @param {string} value - The value to divide
 * @param {string} divisor - The divisor
 * @returns {number} The formatted result
 */
export function divideAndTruncate(value: string, divisor: string, decimalPlaces: number): number {
  try {
    const bigIntValue = BigInt(value);
    // Calculate 10^decimalPlaces as BigInt
    const scale = BigInt(10 ** decimalPlaces);
    // Multiply by scale to get desired decimal places, then divide
    const scaledResult = (bigIntValue * scale) / BigInt(divisor);
    // Convert to number and shift decimal point
    return Number(scaledResult) / Number(scale);
  } catch (e) {
    console.error(`Invalid input: ${value}`, e);
    return 0.0; // Or handle as needed
  }
}

/**
 * Truncates the middle of a text string, showing specified number of characters from start and end
 * @param text The text to truncate
 * @param startLength Number of characters to show from start (default: 6)
 * @param endLength Number of characters to show from end (default: 4)
 * @returns Truncated string with ellipsis in the middle
 */
export function truncateMiddle(text: string, startLength: number = 6, endLength: number = 4): string {
  if (!text) return '';
  if (text.length <= startLength + endLength) return text;

  const start = text.substring(0, startLength);
  const end = text.substring(text.length - endLength);
  return `${start}...${end}`;
}

/**
 * Định dạng một số với số lượng chữ số thập phân động dựa trên độ lớn của nó.
 * - Nếu số >= 1, sử dụng 2 chữ số thập phân.
 * - Nếu số < 1, số chữ số thập phân sẽ tăng lên dựa trên số lượng số 0 đứng đầu,
 *   với tối thiểu là 2 và tối đa là 10.
 *
 * @param value Giá trị số cần định dạng (có thể là number, string, null, hoặc undefined).
 * @returns Một chuỗi đã được định dạng.
 *
 * @example
 * formatDynamicDecimals(3.3333);     // '3.33'
 * formatDynamicDecimals(0.3333);     // '0.33'
 * formatDynamicDecimals(0.03333);    // '0.033'
 * formatDynamicDecimals(0.003333);   // '0.0033'
 * formatDynamicDecimals(0.0000000123); // '0.000000012'
 * formatDynamicDecimals(0);           // '0.00'
 */
export function formatDynamicDecimals(value: number | string | null | undefined): string {
  if (value === null || value === undefined) {
    return '0.00';
  }

  const num = typeof value === 'string' ? parseFloat(value) : value;

  if (isNaN(num) || num === 0) {
    return '0.00';
  }

  const absNum = Math.abs(num);

  // Trường hợp số lớn hơn hoặc bằng 1
  if (absNum >= 1) {
    return num.toFixed(2);
  }

  // Trường hợp số nhỏ hơn 1
  // Tính toán số lượng số 0 đứng ngay sau dấu phẩy
  const leadingZeros = Math.floor(-Math.log10(absNum));

  // Số chữ số thập phân = 2 (cơ bản) + số lượng số 0 đứng đầu
  let precision = 2 + leadingZeros;

  // Giới hạn tối đa là 10 chữ số thập phân
  precision = Math.min(precision, 10);

  return num.toFixed(precision);
}

