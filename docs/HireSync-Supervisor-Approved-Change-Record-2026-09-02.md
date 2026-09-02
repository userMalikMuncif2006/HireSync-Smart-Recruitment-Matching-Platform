# HireSync - Supervisor-Approved Change Record

## ACR-01 - One-Time Email OTP and Employer Company Verification

**Status: APPROVED PRODUCT CHANGE — canonical input for HireSync v1.2**

| Field | Value |
|---|---|
| Project | HireSync - Smart Recruitment Matching Platform |
| Change record | ACR-01 |
| Approval date recorded | 2 September 2026 |
| Approval source | Supervisor/lecturer approval confirmed by Munshif in the HireSync project discussion |
| Evidence boundary | This record documents the approval communicated by the Team Leader. It does not fabricate a signed lecturer document, email, commit, Issue, PR, review, test, CI result, or implementation status. Attach any external approval evidence to the real repository/documentation if available. |
| Supersedes | Only the specific baseline boundaries named below; all unrelated BRD/Second Brain rules remain unchanged |

## 1. Approved Change A - One-Time Email OTP Verification

1. **Employer registration** uses one-time email OTP verification after Employer account/company-basic registration data is submitted.
2. Employer OTP is required **once for email ownership verification**, not on every future login.
3. **Administrator remains securely seeded and is never publicly registerable.** The seeded Administrator completes one-time email OTP verification during first activation/sign-in.
4. Future normal Employer/Administrator sign-ins use email + password; OTP is not a recurring login/MFA requirement.
5. **Job Seeker registration has no OTP requirement** in this approved change.
6. Transactional email capability is permitted only for these authentication OTP messages. This approval does **not** enable application-status emails, contact emails, SMS, marketing mail, recruiter messaging, or any other email/SMS notification workflow.

## 2. Approved Change B - Employer Company Verification

Employer onboarding is separated into two stages:

```text
Employer Sign Up
  -> Form 1: Account + Company Basic Details
  -> One-Time Email OTP
  -> Email Verified
  -> Form 2: Company Verification
  -> Automated Deterministic Verification
  -> Approved / NeedsReview / Rejected
```

### 2.1 Form 1 - Account and company basics

Required baseline data:

- Company legal/display name
- Company location / registered address as defined by the final DTO
- Contact person name
- Contact person designation
- Business Registration Number (BRN)
- Mobile number
- Company website, optional
- Business email
- Password and confirmation

### 2.2 Form 2 - Company verification

Verification uses layered deterministic evidence. It is **not AI/ML**.

Required/eligible factors:

- verified Employer email;
- normalized BRN uniqueness inside HireSync;
- official company-registry BRN/legal-name match when a reliable supported integration is available;
- business-email and company-domain consistency;
- optional DNS TXT domain-ownership verification;
- internal consistency and duplicate-risk checks.

Do **not** scrape a government/company-registry website. Use an official supported integration if one is available. If registry verification is unavailable or inconclusive, use the approved deterministic fallback and/or Admin exception review.

### 2.3 Deterministic decision paths

- **Approved automatically** when a strong approved verification path succeeds. Canonical paths are:
  - verified email + unique BRN + reliable official registry BRN/legal-name match; **or**
  - when reliable registry integration is unavailable, verified email + unique BRN + verified company-domain ownership + business-email domain match + internally consistent company details.
- **NeedsReview** when evidence is incomplete, unavailable, or conflicting but no confirmed hard failure exists.
- **Rejected** when a hard conflict is confirmed (for example duplicate BRN, confirmed registry legal-name/BRN mismatch) or an Administrator rejects a NeedsReview exception with a recorded reason.
- A Rejected Employer may correct permitted verification data and resubmit.
- Additional registration-certificate/evidence upload is a fallback option for uncertain cases only and is not mandatory for every Employer unless separately approved later.

## 3. Status and Authorization Rules

Keep security account state separate from company verification state.

```text
AccountStatus
- Active
- Suspended
```

```text
EmployerVerificationStatus
- Unverified
- Verifying
- Approved
- NeedsReview
- Rejected
```

Rules:

- `AccountStatus` continues to represent security access/suspension.
- An Active Employer whose email is verified may authenticate while company verification is not Approved, but is routed to a restricted verification/status area.
- Employer hiring/business privileges — especially vacancy creation/publishing and Employer applicant workflows — require `EmployerVerificationStatus=Approved` in addition to the existing role/account/ownership rules.
- `NeedsReview` Employers do not wait in a queue for basic authentication; they may sign in to see verification state, but protected Employer hiring capabilities remain blocked.
- Administrator manually reviews **only NeedsReview exceptions**, not every Employer registration.
- Rejected Employers see a safe reason and may correct/resubmit where allowed.
- Changing verification-bound company identity after approval must go through the verification workflow again; ordinary profile editing must not silently bypass company verification.

## 4. Ownership and Contract Impact

### Munshif

Primary ownership remains Foundation/Auth/Authorization/Administrator. Added responsibility:

- C1 email OTP contract and security controls;
- transactional OTP email abstraction/configuration;
- verification-aware shared authorization policy;
- Administrator NeedsReview queue/detail/approve/reject workflow;
- security/error contract and integration coordination.

### Rasadh

Primary ownership remains Employer Profile/Vacancy/Search/Ranked Applicants. Added responsibility:

- Employer Form 1 company fields with C1 coordination;
- Employer Form 2 verification/status UI and Employer-profile verification data;
- Employer-side resubmission/status experience;
- vacancy/hiring surfaces consuming the Approved verification gate.

### Other members

Vimaltan and Abisegha consume the updated C1/authorization behavior but do not own or silently duplicate Employer verification.

## 5. Scope Boundary Amendment

The original BRD exclusion of **email/SMS notifications** remains in force for recruitment/business notifications. ACR-01 creates one narrow approved exception: **transactional one-time email OTP for Employer email ownership verification and seeded Administrator first activation**.

No SMS is introduced. No application/contact notification is emailed. The only stored in-application notification type remains `ApplicationStatusChanged`.

## 6. Required Canonical Updates

ACR-01 is incorporated into:

- `HireSync-Project-Second-Brain-v1.2-FROZEN.md`
- `HireSync-Team-Development-Plan-v1.1-FROZEN.md`
- C1 Authentication contract
- Employer/Admin route, authorization, database, testing, UI, traceability, integration and viva documentation

The v1.1 Second Brain and v1.0 Team Development Plan remain historical freeze records and are not overwritten.
