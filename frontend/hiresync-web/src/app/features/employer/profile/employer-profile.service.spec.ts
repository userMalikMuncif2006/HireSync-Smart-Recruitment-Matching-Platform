import { TestBed } from '@angular/core/testing';
import {
  HttpTestingController,
  provideHttpClientTesting,
} from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import {
  EmployerProfile,
  EmployerVerificationStatus,
  UpdateEmployerProfileRequest,
} from './employer-profile.models';
import { EmployerProfileService } from './employer-profile.service';

describe('EmployerProfileService', () => {
  let service: EmployerProfileService;
  let httpTesting: HttpTestingController;

  const profile: EmployerProfile = {
    id: '11111111-1111-1111-1111-111111111111',
    companyName: 'Acme Lanka',
    description: 'A recruitment-ready software company.',
    location: 'Colombo',
    contactPersonName: 'Nimal Perera',
    contactPersonDesignation: 'HR Manager',
    businessRegistrationNumber: 'BR-1001',
    mobileNumber: '0771234567',
    companyWebsite: 'https://example.com',
    businessEmail: 'hr@example.com',
    employerVerificationStatus:
      EmployerVerificationStatus.Approved,
    isProfileComplete: true,
    isVacancyReady: true,
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        EmployerProfileService,
      ],
    });

    service = TestBed.inject(EmployerProfileService);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpTesting.verify();
  });

  it('gets the authenticated employer profile', () => {
    let actual: EmployerProfile | undefined;

    service.getOwnProfile().subscribe((result) => {
      actual = result;
    });

    const request =
      httpTesting.expectOne('/api/v1/employer/profile');

    expect(request.request.method).toBe('GET');

    request.flush(profile);

    expect(actual).toEqual(profile);
  });

  it('updates the authenticated employer profile', () => {
    const update: UpdateEmployerProfileRequest = {
      companyName: 'Acme Lanka',
      description: 'A recruitment-ready software company.',
      location: 'Colombo',
      contactPersonName: 'Nimal Perera',
      contactPersonDesignation: 'HR Manager',
      businessRegistrationNumber: 'BR-1001',
      mobileNumber: '0771234567',
      companyWebsite: 'https://example.com',
    };

    let actual: EmployerProfile | undefined;

    service.updateOwnProfile(update).subscribe((result) => {
      actual = result;
    });

    const request =
      httpTesting.expectOne('/api/v1/employer/profile');

    expect(request.request.method).toBe('PUT');
    expect(request.request.body).toEqual(update);

    request.flush(profile);

    expect(actual).toEqual(profile);
  });

  it('preserves numeric employer verification status values', () => {
    expect(EmployerVerificationStatus.Pending).toBe(1);
    expect(EmployerVerificationStatus.Approved).toBe(2);
    expect(EmployerVerificationStatus.Rejected).toBe(3);
  });
});