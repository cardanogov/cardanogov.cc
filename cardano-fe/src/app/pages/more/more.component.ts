import { Component, ElementRef, ViewChild } from '@angular/core';

@Component({
  selector: 'app-more',
  imports: [],
  templateUrl: './more.component.html',
  styleUrl: './more.component.scss'
})
export class MoreComponent {
  @ViewChild('delegatevotingpower') delegatevotingpower!: ElementRef;
  @ViewChild('registerDRep') registerDRep!: ElementRef;
  @ViewChild('exampleIPFS') exampleIPFS!: ElementRef;

  scrollToDelegatevotInPower() {
    this.delegatevotingpower.nativeElement.scrollIntoView({ behavior: 'smooth' });
  }

  scrollToRegisterDRep() {
    this.registerDRep.nativeElement.scrollIntoView({ behavior: 'smooth' });
  }

  scrollToExampleIPFS() {
    this.exampleIPFS.nativeElement.scrollIntoView({ behavior: 'smooth' });
  }
}
