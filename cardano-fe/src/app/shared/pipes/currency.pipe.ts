import { Pipe, PipeTransform } from '@angular/core';

@Pipe({
  name: 'currencyFormat'
})
export class CurrencyPipe implements PipeTransform {
  transform(value: number, currencySymbol: string = '$', decimalLength: number = 2): string {
    if (value === null || value === undefined) return '';
    return `${currencySymbol}${value.toFixed(decimalLength)}`;
  }
}
