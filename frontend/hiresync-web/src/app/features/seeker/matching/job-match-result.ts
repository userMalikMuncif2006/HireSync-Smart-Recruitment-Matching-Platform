import {
  Component,
  effect,
  inject,
  input,
  signal,
} from '@angular/core';

import { MatchResult } from './job-match.models';
import { JobMatchService } from './job-match.service';

@Component({
  selector: 'app-job-match-result',
  standalone: true,
  templateUrl: './job-match-result.html',
  styleUrl: './job-match-result.css',
})
export class JobMatchResult {
  readonly vacancyId = input.required<string>();

  readonly result = signal<MatchResult | null>(null);
  readonly loading = signal(false);
  readonly errorMessage = signal<string | null>(null);

  private readonly jobMatchService =
    inject(JobMatchService);

  constructor() {
    effect((onCleanup) => {
      const vacancyId = this.vacancyId().trim();

      this.result.set(null);
      this.errorMessage.set(null);

      if (!vacancyId) {
        this.loading.set(false);
        this.errorMessage.set(
          'A valid vacancy is required to view the match result.',
        );
        return;
      }

      this.loading.set(true);

      const subscription =
        this.jobMatchService
          .getMatch(vacancyId)
          .subscribe({
            next: (result) => {
              this.result.set(result);
              this.loading.set(false);
            },
            error: () => {
              this.result.set(null);
              this.loading.set(false);
              this.errorMessage.set(
                'The match result could not be loaded.',
              );
            },
          });

      onCleanup(() => {
        subscription.unsubscribe();
      });
    });
  }
}
