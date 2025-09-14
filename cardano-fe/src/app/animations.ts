import {
  trigger,
  state,
  style,
  animate,
  transition,
  AnimationTriggerMetadata,
} from '@angular/animations';

// Generic slide-in/out animation
export const slideInOut: AnimationTriggerMetadata = trigger('slideInOut', [
  state('in', style({ height: '*', opacity: 1, transform: 'translateY(0)' })),
  state('out', style({ height: '0', opacity: 0, transform: 'translateY(-10px)' })),
  transition('in => out', animate('300ms ease-in-out')),
  transition('out => in', animate('300ms ease-in-out')),
]);

// Generic fade animation
export const fade: AnimationTriggerMetadata = trigger('fade', [
  state('void', style({ opacity: 0 })),
  state('*', style({ opacity: 1 })),
  transition('void => *', animate('200ms ease-in')),
  transition('* => void', animate('200ms ease-out')),
]);

// Generic scale animation (e.g., for buttons or cards)
export const scale: AnimationTriggerMetadata = trigger('scale', [
  state('normal', style({ transform: 'scale(1)' })),
  state('scaled', style({ transform: 'scale(1.05)' })),
  transition('normal <=> scaled', animate('150ms ease-in-out')),
]);
