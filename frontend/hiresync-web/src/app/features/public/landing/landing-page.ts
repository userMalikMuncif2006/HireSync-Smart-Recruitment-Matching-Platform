import {
  Component,
} from '@angular/core';
import {
  RouterLink,
} from '@angular/router';

@Component({
  selector: 'app-landing-page',
  standalone: true,
  imports: [
    RouterLink,
  ],
  templateUrl: './landing-page.html',
  styleUrls: [
    './landing-page.css',
    './landing-visual.css',
    './landing-sections.css',
    './landing-content.css',
    './landing-responsive.css',
  ],
})
export class LandingPage {}