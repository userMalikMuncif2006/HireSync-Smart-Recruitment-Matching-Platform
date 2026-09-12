import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import { Router } from '@angular/router';
import {
  Observable,
  of,
} from 'rxjs';

import {
  EmployerVacancyPage,
  VacancyStatusResult,
} from './employer-vacancy.models';
import { EmployerVacancyListPage } from './employer-vacancy-list-page';
import { EmployerVacancyService } from './employer-vacancy.service';

describe('EmployerVacancyListPage', () => {
  let fixture:
    ComponentFixture<EmployerVacancyListPage>;

  let service:
    FakeEmployerVacancyService;

  let navigate:
    ReturnType<typeof vi.fn>;

  async function createComponent(
    page:
      EmployerVacancyPage =
        createVacancyPage(),
  ): Promise<void> {
    service =
      new FakeEmployerVacancyService();

    service.vacanciesResponse =
      of(page);

    navigate =
      vi.fn().mockResolvedValue(true);

    await TestBed
      .configureTestingModule({
        imports: [
          EmployerVacancyListPage,
        ],
        providers: [
          {
            provide:
              EmployerVacancyService,
            useValue:
              service,
          },
          {
            provide:
              Router,
            useValue: {
              navigate,
            },
          },
        ],
      })
      .compileComponents();

    fixture =
      TestBed.createComponent(
        EmployerVacancyListPage,
      );

    fixture.detectChanges();

    await fixture.whenStable();

    fixture.detectChanges();
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('loads and renders the Employer vacancy portfolio', async () => {
    await createComponent();

    expect(service.vacancyQueries)
      .toEqual([
        {
          status: null,
          page: 1,
          pageSize: 20,
        },
      ]);

    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain('Backend Developer');

    expect(text)
      .toContain('Colombo');

    expect(text)
      .toContain('Open');
  });

  it('applies the Open vacancy filter from page one', async () => {
    await createComponent();

    fixture.componentInstance
      .statusFilter
      .setValue('open');

    fixture.componentInstance
      .applyFilter();

    expect(
      service.vacancyQueries.at(-1),
    ).toEqual({
      status: 1,
      page: 1,
      pageSize: 20,
    });
  });

  it('closes using the current row version and reloads', async () => {
    const page =
      createVacancyPage();

    await createComponent(page);

    const vacancy =
      page.items[0];

    fixture.componentInstance
      .requestClose(vacancy.id);

    fixture.componentInstance
      .confirmClose(vacancy);

    expect(service.closeCalls)
      .toEqual([
        {
          vacancyId:
            vacancy.id,
          rowVersion:
            'AQIDBA==',
        },
      ]);

    expect(
      service.vacancyQueries.length,
    ).toBe(2);
  });

  it('navigates to vacancy creation', async () => {
    await createComponent();

    fixture.componentInstance
      .createVacancy();

    expect(navigate)
      .toHaveBeenCalledWith([
        '/employer/vacancies/new',
      ]);
  });

  it('navigates to vacancy editing', async () => {
    await createComponent();

    fixture.componentInstance
      .editVacancy('vacancy-1');

    expect(navigate)
      .toHaveBeenCalledWith([
        '/employer/vacancies',
        'vacancy-1',
        'edit',
      ]);
  });

  it('navigates to the vacancy ranked-applicant view', async () => {
    await createComponent();

    fixture.componentInstance
      .viewApplicants('vacancy-1');

    expect(navigate)
      .toHaveBeenCalledWith([
        '/employer/vacancies',
        'vacancy-1',
        'applicants',
      ]);
  });
});

function createVacancyPage():
  EmployerVacancyPage {
  return {
    items: [
      {
        id: 'vacancy-1',
        title: 'Backend Developer',
        location: 'Colombo',
        status: 1,
        publishedAtUtc:
          '2026-09-12T08:00:00Z',
        updatedAtUtc:
          '2026-09-12T09:00:00Z',
        closedAtUtc: null,
        rowVersion: 'AQIDBA==',
      },
    ],
    page: 1,
    pageSize: 20,
    totalCount: 1,
  };
}

class FakeEmployerVacancyService {
  vacanciesResponse:
    Observable<EmployerVacancyPage> =
      of(createVacancyPage());

  closeResponse:
    Observable<VacancyStatusResult> =
      of({
        id: 'vacancy-1',
        status: 2,
        closedAtUtc:
          '2026-09-12T12:00:00Z',
        rowVersion:
          'BQYHCA==',
      });

  readonly vacancyQueries:
    Array<{
      status: 1 | 2 | null;
      page: number;
      pageSize: number;
    }> = [];

  readonly closeCalls:
    Array<{
      vacancyId: string;
      rowVersion: string;
    }> = [];

  getVacancies(
    status: 1 | 2 | null,
    page: number,
    pageSize: number,
  ): Observable<EmployerVacancyPage> {
    this.vacancyQueries.push({
      status,
      page,
      pageSize,
    });

    return this.vacanciesResponse;
  }

  closeVacancy(
    vacancyId: string,
    rowVersion: string,
  ): Observable<VacancyStatusResult> {
    this.closeCalls.push({
      vacancyId,
      rowVersion,
    });

    return this.closeResponse;
  }
}