import { Component, ContentChild, ElementRef, Input, TemplateRef, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';

@Component({
  selector: 'app-carousel-tag',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './carousel-tag.component.html',
  styleUrl: './carousel-tag.component.scss'
})
export class CarouselTagComponent {
  @Input() items: any[] = [];
  @ContentChild('template') template: TemplateRef<any> | null = null;
  @ViewChild('carousel', { static: false }) carousel!: ElementRef;

  scrollLeft() {
    const carousel = this.carousel.nativeElement;
    carousel.scrollLeft -= 200;
  }

  scrollRight() {
    const carousel = this.carousel.nativeElement;
    carousel.scrollLeft += 200;
  }
}
