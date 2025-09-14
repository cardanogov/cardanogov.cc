import { ComponentFixture, TestBed } from '@angular/core/testing';

import { CarouselTagComponent } from './carousel-tag.component';

describe('CarouselTagComponent', () => {
  let component: CarouselTagComponent;
  let fixture: ComponentFixture<CarouselTagComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [CarouselTagComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(CarouselTagComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
