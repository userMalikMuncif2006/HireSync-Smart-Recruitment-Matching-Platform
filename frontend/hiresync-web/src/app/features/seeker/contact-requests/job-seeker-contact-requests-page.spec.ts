import {
  HttpErrorResponse,
} from '@angular/common/http';
import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  Observable,
  of,
  throwError,
} from 'rxjs';

import {
  ContactRequestResult,
  ContactRequestStatus,
  JobSeekerContactRequest,
  RespondContactRequestPayload,
} from './job-seeker-contact-requests.models';
import {
  JobSeekerContactRequestsPage,
} from './job-seeker-contact-requests-page';
import {
  JobSeekerContactRequestsService,
} from './job-seeker-contact-requests.service';

describe(
  'JobSeekerContactRequestsPage',
  () => {
    let fixture:
      ComponentFixture<
        JobSeekerContactRequestsPage
      >;

    let service:
      FakeContactRequestsService;

    async function createComponent(
      response:
        Observable<
          JobSeekerContactRequest[]
        > = of([
          createRequest(),
        ]),
    ): Promise<void> {
      service =
        new FakeContactRequestsService();

      service.listResponse =
        response;

      await TestBed
        .configureTestingModule({
          imports: [
            JobSeekerContactRequestsPage,
          ],
          providers: [
            {
              provide:
                JobSeekerContactRequestsService,
              useValue:
                service,
            },
          ],
        })
        .compileComponents();

      fixture =
        TestBed.createComponent(
          JobSeekerContactRequestsPage,
        );

      fixture.detectChanges();

      await fixture.whenStable();

      fixture.detectChanges();
    }

    afterEach(() => {
      vi.restoreAllMocks();
    });

    it('renders pending contact request data without personal contact details', async () => {
      await createComponent();

      const text =
        fixture.nativeElement
          .textContent as string;

      expect(text)
        .toContain(
          'HireSync Employer',
        );

      expect(text)
        .toContain(
          'Backend Developer',
        );

      expect(text)
        .toContain('Pending');

      expect(text)
        .toContain('Accept');

      expect(text)
        .toContain('Decline');

      expect(service.loads)
        .toBe(1);
    });

    it('renders the empty state', async () => {
      await createComponent(
        of([]),
      );

      expect(
        fixture.nativeElement
          .textContent as string,
      ).toContain(
        'No contact requests',
      );
    });

    it('renders a load error state', async () => {
      await createComponent(
        throwError(
          () =>
            new HttpErrorResponse({
              status: 500,
            }),
        ),
      );

      expect(
        fixture.nativeElement
          .textContent as string,
      ).toContain(
        'Contact requests unavailable',
      );
    });

    it('accepts a pending request with its current row version and re-fetches', async () => {
      await createComponent();

      service.respondResponse =
        of({
          id:
            'contact-1',
          jobApplicationId:
            'application-1',
          status:
            ContactRequestStatus.Accepted,
          requestedAtUtc:
            '2026-09-13T03:00:00Z',
          respondedAtUtc:
            '2026-09-13T03:05:00Z',
          rowVersion:
            'BQYHCA==',
        });

      service.listResponse =
        of([
          createRequest(
            ContactRequestStatus.Accepted,
            'BQYHCA==',
            '2026-09-13T03:05:00Z',
          ),
        ]);

      const button =
        fixture.nativeElement
          .querySelector(
            '.accept-button',
          ) as HTMLButtonElement;

      button.click();
      fixture.detectChanges();

      expect(service.responses)
        .toEqual([
          {
            id:
              'contact-1',
            payload: {
              status:
                ContactRequestStatus.Accepted,
              rowVersion:
                'AQIDBA==',
            },
          },
        ]);

      expect(service.loads)
        .toBe(2);

      expect(
        fixture.componentInstance
          .requests()[0]
          .status,
      ).toBe(
        ContactRequestStatus.Accepted,
      );

      expect(
        fixture.componentInstance
          .responseSuccessMessage(),
      ).toBe(
        'Contact request accepted.',
      );
    });

    it('declines a pending request only after confirmation', async () => {
      await createComponent();

      const confirm =
        vi.spyOn(
          window,
          'confirm',
        );

      confirm.mockReturnValue(
        false,
      );

      const request =
        fixture.componentInstance
          .requests()[0];

      fixture.componentInstance
        .respond(
          request,
          ContactRequestStatus.Declined,
        );

      expect(service.responses)
        .toHaveLength(0);

      confirm.mockReturnValue(
        true,
      );

      service.listResponse =
        of([
          createRequest(
            ContactRequestStatus.Declined,
            'BQYHCA==',
            '2026-09-13T03:05:00Z',
          ),
        ]);

      fixture.componentInstance
        .respond(
          request,
          ContactRequestStatus.Declined,
        );

      expect(service.responses)
        .toHaveLength(1);

      expect(
        service.responses[0]
          .payload.status,
      ).toBe(
        ContactRequestStatus.Declined,
      );
    });

    it('does not allow another response after the request is complete', async () => {
      await createComponent(
        of([
          createRequest(
            ContactRequestStatus.Accepted,
            'BQYHCA==',
            '2026-09-13T03:05:00Z',
          ),
        ]),
      );

      expect(
        fixture.nativeElement
          .querySelector(
            '.response-actions button',
          ),
      ).toBeNull();

      expect(
        fixture.nativeElement
          .textContent as string,
      ).toContain(
        'This request is complete.',
      );
    });

    it('surfaces backend ProblemDetails when responding fails', async () => {
      await createComponent();

      service.respondResponse =
        throwError(
          () =>
            new HttpErrorResponse({
              status: 409,
              error: {
                detail:
                  'Only a Pending request can be accepted or declined.',
              },
            }),
        );

      fixture.componentInstance
        .respond(
          fixture.componentInstance
            .requests()[0],
          ContactRequestStatus.Accepted,
        );

      fixture.detectChanges();

      expect(
        fixture.componentInstance
          .responseErrorMessage(),
      ).toBe(
        'Only a Pending request can be accepted or declined.',
      );

      expect(service.loads)
        .toBe(1);
    });
  },
);


function createRequest(
  status:
    ContactRequestStatus =
      ContactRequestStatus.Pending,
  rowVersion = 'AQIDBA==',
  respondedAtUtc:
    string | null = null,
): JobSeekerContactRequest {
  return {
    id:
      'contact-1',
    jobApplicationId:
      'application-1',
    vacancyId:
      'vacancy-1',
    vacancyTitle:
      'Backend Developer',
    employerCompanyName:
      'HireSync Employer',
    status,
    requestedAtUtc:
      '2026-09-13T03:00:00Z',
    respondedAtUtc,
    rowVersion,
  };
}


class FakeContactRequestsService {
  listResponse:
    Observable<
      JobSeekerContactRequest[]
    > =
      of([
        createRequest(),
      ]);

  respondResponse:
    Observable<ContactRequestResult> =
      of({
        id:
          'contact-1',
        jobApplicationId:
          'application-1',
        status:
          ContactRequestStatus.Accepted,
        requestedAtUtc:
          '2026-09-13T03:00:00Z',
        respondedAtUtc:
          '2026-09-13T03:05:00Z',
        rowVersion:
          'BQYHCA==',
      });

  loads = 0;

  readonly responses:
    Array<{
      id: string;
      payload:
        RespondContactRequestPayload;
    }> = [];

  getOwnContactRequests():
    Observable<
      JobSeekerContactRequest[]
    > {
    this.loads += 1;

    return this.listResponse;
  }

  respondToContactRequest(
    id: string,
    payload:
      RespondContactRequestPayload,
  ): Observable<ContactRequestResult> {
    this.responses.push({
      id,
      payload,
    });

    return this.respondResponse;
  }
}
