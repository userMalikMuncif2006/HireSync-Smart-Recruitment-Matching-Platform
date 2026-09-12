import {
  ComponentFixture,
  TestBed,
} from '@angular/core/testing';
import {
  ActivatedRoute,
  convertToParamMap,
  Params,
  Router,
} from '@angular/router';
import {
  BehaviorSubject,
  Observable,
  of,
} from 'rxjs';

import {
  PublicVacancyPage,
  VacancySearchQuery,
} from './vacancy-search.models';
import { VacancySearchPage } from './vacancy-search-page';
import { VacancySearchService } from './vacancy-search.service';

describe('VacancySearchPage', () => {
  let fixture:
    ComponentFixture<VacancySearchPage>;

  let service:
    FakeVacancySearchService;

  let navigate:
    ReturnType<typeof vi.fn>;

  async function createComponent(
    params: Params = {},
    response:
      PublicVacancyPage = createPage(),
  ): Promise<void> {
    const queryParamMap =
      new BehaviorSubject(
        convertToParamMap(params),
      );

    service =
      new FakeVacancySearchService();

    service.response =
      of(response);

    navigate =
      vi.fn().mockResolvedValue(true);

    await TestBed
      .configureTestingModule({
        imports: [
          VacancySearchPage,
        ],
        providers: [
          {
            provide:
              VacancySearchService,
            useValue:
              service,
          },
          {
            provide:
              ActivatedRoute,
            useValue: {
              queryParamMap:
                queryParamMap.asObservable(),
            },
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
        VacancySearchPage,
      );

    fixture.detectChanges();

    await fixture.whenStable();

    fixture.detectChanges();
  }

  afterEach(() => {
    vi.restoreAllMocks();
  });

  it('loads URL query parameters and renders the backend match score unchanged', async () => {
    await createComponent(
      {
        q: 'backend',
        location: 'Colombo',
        sort: 'Match',
        page: '2',
      },
      createPage(
        2,
        25,
        87.5,
      ),
    );

    expect(service.queries)
      .toEqual([
        {
          q: 'backend',
          location: 'Colombo',
          sort: 'Match',
          page: 2,
          pageSize: 20,
        },
      ]);

    expect(
      fixture.componentInstance
        .form
        .getRawValue(),
    ).toEqual({
      q: 'backend',
      location: 'Colombo',
      sort: 'Match',
    });

    const text =
      fixture.nativeElement
        .textContent as string;

    expect(text)
      .toContain('Backend Developer');

    expect(text)
      .toContain('HireSync Employer');

    expect(text)
      .toContain('87.50%');

    expect(text)
      .toContain(
        'Ranked using your current structured profile',
      );
  });

  it('falls back to page one and Newest for invalid URL values', async () => {
    await createComponent({
      sort: 'Unknown',
      page: '0',
    });

    expect(service.queries[0])
      .toEqual({
        q: undefined,
        location: undefined,
        sort: 'Newest',
        page: 1,
        pageSize: 20,
      });

    expect(
      fixture.componentInstance
        .form.controls.sort.value,
    ).toBe('Newest');
  });

  it('writes trimmed filters and sort back to the URL', async () => {
    await createComponent();

    fixture.componentInstance
      .form
      .setValue({
        q: '  dotnet  ',
        location: '  Colombo  ',
        sort: 'Match',
      });

    fixture.componentInstance
      .applyFilters();

    expect(navigate)
      .toHaveBeenCalledWith(
        [],
        expect.objectContaining({
          queryParams: {
            q: 'dotnet',
            location: 'Colombo',
            sort: 'Match',
            page: 1,
          },
        }),
      );
  });
});

function createPage(
  page = 1,
  totalCount = 1,
  matchScore:
    number | null = null,
): PublicVacancyPage {
  return {
    items: [
      {
        id:
          '11111111-1111-1111-1111-111111111111',
        title:
          'Backend Developer',
        companyName:
          'HireSync Employer',
        location:
          'Colombo',
        minimumExperienceMonths:
          24,
        requiredEducationLevel:
          5,
        publishedAtUtc:
          '2026-09-12T10:00:00Z',
        matchScore,
      },
    ],
    page,
    pageSize: 20,
    totalCount,
  };
}

class FakeVacancySearchService {
  response:
    Observable<PublicVacancyPage> =
      of(createPage());

  readonly queries:
    VacancySearchQuery[] = [];

  search(
    query: VacancySearchQuery,
  ): Observable<PublicVacancyPage> {
    this.queries.push({
      ...query,
    });

    return this.response;
  }
}