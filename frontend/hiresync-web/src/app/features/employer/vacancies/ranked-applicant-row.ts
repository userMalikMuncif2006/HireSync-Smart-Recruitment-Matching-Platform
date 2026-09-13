import {
  DatePipe,
} from '@angular/common';
import {
  Component,
  EventEmitter,
  Input,
  Output,
} from '@angular/core';

import {
  InitialsPipe,
} from '../../../shared/pipes/initials.pipe';
import {
  ApplicationStatus,
  ContactRequestStatus,
  RankedApplicant,
} from './employer-vacancy.models';

@Component({
  selector: 'app-ranked-applicant-row',
  standalone: true,
  imports: [
    DatePipe,
    InitialsPipe,
  ],
  templateUrl:
    './ranked-applicant-row.html',
  styleUrl:
    './ranked-applicant-row.css',
})
export class RankedApplicantRowComponent {
  @Input({
    required: true,
  })
  applicant!: RankedApplicant;

  @Input({
    required: true,
  })
  selectedStatus!:
    ApplicationStatus;

  @Input()
  updatingApplicationId:
    string | null = null;

  @Input()
  creatingContactApplicationId:
    string | null = null;

  @Output()
  readonly statusSelected =
    new EventEmitter<
      ApplicationStatus
    >();

  @Output()
  readonly updateRequested =
    new EventEmitter<void>();

  @Output()
  readonly contactRequested =
    new EventEmitter<void>();

  applicationStatusLabel(
    status: ApplicationStatus,
  ): string {
    switch (status) {
      case 1:
        return 'Applied';

      case 2:
        return 'Under review';

      case 3:
        return 'Shortlisted';

      case 4:
        return 'Selected';

      case 5:
        return 'Rejected';
    }
  }

  contactStatusLabel(
    status:
      ContactRequestStatus |
      null,
  ): string {
    switch (status) {
      case 1:
        return 'Contact pending';

      case 2:
        return 'Contact accepted';

      case 3:
        return 'Contact declined';

      default:
        return 'No contact request';
    }
  }

  scoreLabel(
    score: number,
  ): string {
    return `${score.toFixed(2)}%`;
  }

  componentScoreLabel(
    score: number,
    maximum: number,
  ): string {
    return `${score.toFixed(2)} / ${maximum}`;
  }

  isTerminalStatus(): boolean {
    return (
      this.applicant.status === 4 ||
      this.applicant.status === 5
    );
  }

  canUpdateStatus(): boolean {
    return (
      !this.isTerminalStatus() &&
      this.selectedStatus !==
        this.applicant.status &&
      this.updatingApplicationId ===
        null
    );
  }

  canCreateContactRequest(): boolean {
    return (
      this.applicant
        .contactRequestStatus ===
        null &&
      this
        .creatingContactApplicationId ===
        null
    );
  }

  selectStatus(
    value: string,
  ): void {
    this.statusSelected.emit(
      Number(
        value,
      ) as ApplicationStatus,
    );
  }
}