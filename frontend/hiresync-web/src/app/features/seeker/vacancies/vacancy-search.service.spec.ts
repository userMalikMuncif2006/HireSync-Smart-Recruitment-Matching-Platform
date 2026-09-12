import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import {
  PublicVacancyPage,
} from './vacancy-search.models';
import { VacancySearchService } from './vacancy-search.service';

describe('VacancySearchService', () => {
  let service: VacancySearchService;
  let httpTesting: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        VacancySearchService,
      ],
    });

    service =
      TestBed.inject(VacancySearchService);

    httpTesting =
      TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('sends the canonical match-search query parameters', () => {
    const expected: PublicVacancyPage = {
      items: [],
      page: 2,
      pageSize: 20,
      totalCount: 0,
    };

    let actual:
      PublicVacancyPage | undefined;

    service.search({
      q: '  backend  ',
      location: '  Colombo  ',
      sort: 'Match',
      page: 2,
      pageSize: 20,
    }).subscribe((result) => {
      actual = result;
    });

    const request =
      httpTesting.expectOne(
        (candidate) =>
          candidate.url ===
          '/api/v1/vacancies',
      );

    expect(request.request.method)
      .toBe('GET');

    expect(request.request.params.get('q'))
      .toBe('backend');

    expect(
      request.request.params.get('location'),
    ).toBe('Colombo');

    expect(request.request.params.get('sort'))
      .toBe('Match');

    expect(request.request.params.get('page'))
      .toBe('2');

    expect(
      request.request.params.get('pageSize'),
    ).toBe('20');

    request.flush(expected);

    expect(actual)
      .toEqual(expected);
  });

  it('omits blank optional filters', () => {
    service.search({
      q: '   ',
      location: '',
      sort: 'Newest',
      page: 1,
      pageSize: 20,
    }).subscribe();

    const request =
      httpTesting.expectOne(
        (candidate) =>
          candidate.url ===
          '/api/v1/vacancies',
      );

    expect(request.request.params.has('q'))
      .toBe(false);

    expect(
      request.request.params.has('location'),
    ).toBe(false);

    expect(request.request.params.get('sort'))
      .toBe('Newest');

    expect(request.request.params.get('page'))
      .toBe('1');

    request.flush({
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
    });
  });
});