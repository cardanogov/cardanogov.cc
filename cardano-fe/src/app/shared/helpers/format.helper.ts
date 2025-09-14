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

