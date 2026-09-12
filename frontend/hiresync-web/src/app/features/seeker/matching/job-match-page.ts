import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute } from '@angular/router';
import { map } from 'rxjs';

import { JobMatchResult } from './job-match-result';

@Component({
  selector: 'app-job-match-page',
  standalone: true,
  imports: [JobMatchResult],
  templateUrl: './job-match-page.html',
  styleUrl: './job-match-page.css',
})
export class JobMatchPage {
  private readonly route = inject(ActivatedRoute);

  readonly vacancyId = toSignal(
    this.route.paramMap.pipe(
      map(
        (params) =>
          params.get('vacancyId') ?? '',
      ),
    ),
    {
      initialValue: '',
    },
  );
}
