import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Observable, of, throwError } from 'rxjs';

import {
  EmployerProfile,
  EmployerVerificationStatus,
  UpdateEmployerProfileRequest,
} from './employer-profile.models';
import { EmployerProfilePage } from './employer-profile-page';
import { EmployerProfileService } from './employer-profile.service';

describe('EmployerProfilePage', () => {
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

  let fakeService: FakeEmployerProfileService;

  beforeEach(async () => {
    fakeService = new FakeEmployerProfileService();

    await TestBed.configureTestingModule({
      imports: [EmployerProfilePage],
      providers: [
        {
          provide: EmployerProfileService,
          useValue: fakeService,
        },
      ],
    }).compileComponents();
  });

  function createPage(): {
    fixture: ComponentFixture<EmployerProfilePage>;
    component: EmployerProfilePage;
  } {
    const fixture =
      TestBed.createComponent(EmployerProfilePage);

    fixture.detectChanges();

    return {
      fixture,
      component: fixture.componentInstance,
    };
  }

  it('loads the authenticated employer profile into the form', () => {
    const { fixture, component } = createPage();

    expect(component.form.getRawValue()).toEqual({
      companyName: 'Acme Lanka',
      description: 'A recruitment-ready software company.',
      location: 'Colombo',
      contactPersonName: 'Nimal Perera',
      contactPersonDesignation: 'HR Manager',
      businessRegistrationNumber: 'BR-1001',
      mobileNumber: '0771234567',
      companyWebsite: 'https://example.com',
    });

    const text =
      fixture.nativeElement.textContent as string;

    expect(text).toContain('hr@example.com');
    expect(text).toContain('Approved');
    expect(text).toContain('Vacancy ready');
  });

  it('does not submit an invalid profile form', () => {
    const { component } = createPage();

    component.form.controls.companyName.setValue('');

    component.save();

    expect(fakeService.updateRequests).toHaveLength(0);
    expect(
      component.form.controls.companyName.touched,
    ).toBe(true);
  });

  it('trims editable values and converts an empty website to null', () => {
    const { component } = createPage();

    component.form.patchValue({
      companyName: '  Acme Lanka Updated  ',
      location: '  Colombo  ',
      companyWebsite: '   ',
    });

    component.save();

    expect(fakeService.updateRequests).toHaveLength(1);

    expect(fakeService.updateRequests[0]).toEqual({
      companyName: 'Acme Lanka Updated',
      description: 'A recruitment-ready software company.',
      location: 'Colombo',
      contactPersonName: 'Nimal Perera',
      contactPersonDesignation: 'HR Manager',
      businessRegistrationNumber: 'BR-1001',
      mobileNumber: '0771234567',
      companyWebsite: null,
    });

    expect(component.successMessage()).toBe(
      'Employer profile updated successfully.',
    );
  });

  it('shows a load error when the profile request fails', () => {
    fakeService.getResult = throwError(
      () => new Error('load failed'),
    );

    const { fixture, component } = createPage();

    expect(component.profile()).toBeNull();
    expect(component.isLoading()).toBe(false);
    expect(component.errorMessage()).toBe(
      'Employer profile could not be loaded.',
    );

    expect(
      fixture.nativeElement.textContent,
    ).toContain('Employer profile could not be loaded.');
  });

  class FakeEmployerProfileService {
    getResult: Observable<EmployerProfile> = of(profile);

    updateResult: Observable<EmployerProfile> = of(profile);

    readonly updateRequests:
      UpdateEmployerProfileRequest[] = [];

    getOwnProfile(): Observable<EmployerProfile> {
      return this.getResult;
    }

    updateOwnProfile(
      request: UpdateEmployerProfileRequest,
    ): Observable<EmployerProfile> {
      this.updateRequests.push(request);
      return this.updateResult;
    }
  }
});