# HireSync --- Smart Recruitment Matching Platform

## Project Second Brain v2.0 FROZEN

Version: 2.0\
Status: FROZEN\
Document Type: Canonical Project Source

------------------------------------------------------------------------

# 1. Project Overview

HireSync is a Smart Recruitment Matching Platform connecting:

-   Job Seekers
-   Employers
-   Administrators

The platform provides structured profiles, vacancy management,
deterministic matching, application workflows and controlled contact
requests.

CV files are stored securely and are not used for AI/NLP/OCR matching.

------------------------------------------------------------------------

# 2. Source Authority

Decision priority:

1.  Official Smart Recruitment Matching Platform BRD v2.0
2.  Approved supervisor decisions
3.  HireSync Second Brain v2.0
4.  HireSync Team Development Plan v2.0
5.  Current GitHub repository state

------------------------------------------------------------------------

# 3. Technology Stack

## Backend

-   ASP.NET Core Web API
-   .NET 8
-   C#
-   Entity Framework Core 8
-   SQL Server 2022 Express
-   REST API
-   Swagger/OpenAPI

## Frontend

-   Angular 19
-   TypeScript
-   Standalone Components
-   Angular Router
-   HttpClient
-   Tailwind CSS 4
-   Selective Angular Material
-   Lucide Angular
-   Reactive Forms
-   Template-driven Forms
-   Signals + Services
-   Vitest

------------------------------------------------------------------------

# 4. Architecture

Pragmatic Clean Architecture:

Domain\
↓\
Application\
↓\
Infrastructure\
↓\
API

Domain has no ASP.NET Core, EF Core or Identity dependency.

------------------------------------------------------------------------

# 5. Authentication and Security

Roles:

-   JobSeeker
-   Employer
-   Administrator

Authentication:

-   ASP.NET Core Identity
-   JWT

Backend authorization is the source of truth.

------------------------------------------------------------------------

# 6. OTP and Employer Verification

Employer flow:

Registration\
↓\
Email OTP\
↓\
Company Verification\
↓\
Platform Access

Administrator:

Seeded account\
↓\
First activation OTP\
↓\
Admin access

Employer verification statuses:

-   Pending
-   Approved
-   Rejected

------------------------------------------------------------------------

# 7. Deterministic Matching

Matching is:

-   server-side
-   deterministic
-   explainable

Weights:

  Factor         Weight
  ------------ --------
  Skills            50%
  Experience        25%
  Education         15%
  Location          10%

Output:

-   total score
-   component scores
-   matched skills
-   missing skills

CV never affects matching.

------------------------------------------------------------------------

# 8. Business Workflows

## Job Seeker

-   Profile management
-   Skill management
-   CV upload
-   Job search
-   Match viewing
-   Applying
-   Application tracking
-   Contact requests
-   Notifications

## Employer

-   Company profile
-   Verification
-   Vacancy management
-   Applicant review
-   Application status updates

## Administrator

-   Dashboard
-   User management
-   Employer verification
-   Account suspension/reactivation

------------------------------------------------------------------------

# 9. Application Workflow

Lifecycle:

Applied\
↓\
UnderReview\
↓\
Shortlisted\
↓\
Selected

Rejected

Rules:

-   Apply once only
-   Duplicate applications blocked
-   ApplicationStatusChanged notification created

------------------------------------------------------------------------

# 10. Contact Request

Flow:

Employer → Contact Request → Job Seeker → Accept/Decline

No:

-   chat
-   personal contact sharing
-   attachments

------------------------------------------------------------------------

# 11. C1-C6 Ownership

## C1 Authentication

Owner: Munshif

## C2 Canonical Skill

Owner: Vimaltan

## C3 Matching Input

Owners: Vimaltan, Rasadh, Abisegha

## C4 Matching Output

Owner: Abisegha

## C5 Application/Notification

Owner: Abisegha

## C6 Contact Request

Owner: Abisegha

------------------------------------------------------------------------

# 12. Member Ownership

## Munshif

-   Foundation
-   Authentication
-   Authorization
-   Administration
-   Integration

## Vimaltan

-   Job Seeker profile
-   Skills
-   CV workflow
-   Application tracking
-   Notifications

## Rasadh

-   Employer profile
-   Vacancy lifecycle
-   Job search
-   Applicant workflow

## Abisegha

-   Matching engine
-   Match output
-   Application workflow
-   Contact lifecycle

------------------------------------------------------------------------

# 13. GitHub Workflow

Branches:

-   main
-   develop
-   feature/\*
-   fix/\*
-   test/\*
-   docs/\*

Flow:

Issue → Branch → Implementation → Tests → PR → Review → Merge

Rules:

-   Genuine commits only
-   Genuine reviews only
-   No fabricated contribution history

------------------------------------------------------------------------

# 14. Definition of Done

Feature requires:

-   completed implementation
-   tests executed
-   documentation updated
-   GitHub Issue/PR traceability
-   successful integration verification

------------------------------------------------------------------------

# 15. Pending Decisions

  Item                             Status
  -------------------------------- ------------------
  Employer applicant CV access     Pending approval
  Skill weight effect              Pending decision
  Landing route                    Pending decision
  Contact message interpretation   Pending decision
  Experience entity depth          Pending decision
  Education entity depth           Pending decision

------------------------------------------------------------------------

# Freeze Status

Document:

HireSync-Project-Second-Brain-v2.0-FROZEN.md

Status:

FROZEN

Role:

Current canonical HireSync project source.
