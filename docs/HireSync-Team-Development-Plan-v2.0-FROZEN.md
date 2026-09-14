# HireSync --- Smart Recruitment Matching Platform

## Team Development Plan v2.0 FROZEN

Version: 2.0\
Status: FROZEN\
Document Type: Official Implementation and Ownership Plan

------------------------------------------------------------------------

# 1. Document Information

Project: HireSync --- Smart Recruitment Matching Platform

Based On:

-   Official Smart Recruitment Matching Platform BRD v2.0
-   HireSync Project Second Brain v2.0 FROZEN
-   Approved project decisions

Purpose:

Define:

-   member ownership
-   implementation responsibilities
-   dependencies
-   GitHub workflow
-   testing responsibilities
-   integration process

------------------------------------------------------------------------

# 2. Team Structure

  Member     Role
  ---------- ---------------------------------------
  Munshif    Team Leader / Integration Coordinator
  Vimaltan   Job Seeker Module Owner
  Rasadh     Employer Module Owner
  Abisegha   Matching & Workflow Owner

------------------------------------------------------------------------

# 3. Development Principles

All members must:

-   use GitHub branches
-   create genuine commits
-   create genuine pull requests
-   participate in reviews
-   test their changes
-   communicate contract changes

No silent ownership changes are allowed.

------------------------------------------------------------------------

# 4. Repository Workflow

Repository style:

Monorepo

Branches:

-   main
-   develop
-   feature/\*
-   fix/\*
-   test/\*
-   docs/\*

Workflow:

Issue

↓

Feature Branch

↓

Implementation

↓

Testing

↓

Pull Request

↓

Peer Review

↓

Merge to develop

↓

Integration Verification

↓

Release to main

------------------------------------------------------------------------

# 5. Review Rotation

  Author     Reviewer
  ---------- ----------
  Munshif    Abisegha
  Vimaltan   Munshif
  Rasadh     Vimaltan
  Abisegha   Rasadh

Self approval is not allowed.

------------------------------------------------------------------------

# 6. Member Ownership

## Munshif --- Platform Foundation, Security & Administration

Responsibilities:

-   repository foundation
-   backend solution foundation
-   frontend workspace foundation
-   authentication
-   authorization
-   JWT security
-   administrator module
-   employer verification approval
-   integration coordination

Contract:

C1 Authentication

------------------------------------------------------------------------

## Vimaltan --- Job Seeker Module

Responsibilities:

-   Job Seeker profile
-   experience
-   education
-   location
-   profile readiness
-   canonical skills
-   CV upload/replacement
-   application tracking
-   notification retrieval

Contracts:

C2 Canonical Skill

Consumes:

C5 Application/Notification

------------------------------------------------------------------------

## Rasadh --- Employer & Vacancy Module

Responsibilities:

-   employer profile
-   company information
-   vacancy lifecycle
-   job search
-   filtering
-   employer applicant workflow

Consumes:

C4 Matching Output

------------------------------------------------------------------------

## Abisegha --- Matching & Workflow Module

Responsibilities:

-   deterministic matching engine
-   match scores
-   matched skills
-   missing skills
-   apply-once workflow
-   application status lifecycle
-   contact request lifecycle

Contracts:

C3 Matching Input

C4 Matching Output

C6 Contact Request

------------------------------------------------------------------------

# 7. Matching Ownership

Formula:

  Factor         Weight
  ------------ --------
  Skills            50%
  Experience        25%
  Education         15%
  Location          10%

Rules:

-   server-side calculation
-   deterministic output
-   explainable results
-   CV does not affect matching

------------------------------------------------------------------------

# 8. Cross Member Dependencies

Authentication:

Owner: Munshif

Used by: All modules

Candidate Skills:

Owner: Vimaltan

Used by: Rasadh and Abisegha

Vacancy Data:

Owner: Rasadh

Used by: Matching workflow

Matching Output:

Owner: Abisegha

Used by: Employer and Job Seeker workflows

------------------------------------------------------------------------

# 9. Implementation Order

## Phase 1 --- Foundation

Owner:

Munshif

Includes:

-   repository structure
-   architecture
-   authentication
-   authorization

## Phase 2 --- Core Modules

Parallel development:

Vimaltan:

-   Job Seeker profile
-   skills
-   CV workflow

Rasadh:

-   Employer profile
-   vacancies
-   job search

## Phase 3 --- Matching and Workflows

Abisegha:

-   matching engine
-   applications
-   contact lifecycle

## Phase 4 --- Integration

All members:

-   API integration
-   frontend integration
-   testing
-   fixes

------------------------------------------------------------------------

# 10. Testing Responsibilities

Each owner provides:

-   unit tests
-   frontend tests
-   API validation
-   critical workflow testing

------------------------------------------------------------------------

# 11. Definition of Done

A feature is complete when:

## Code

-   implementation completed
-   architecture rules followed

## Testing

-   tests created
-   tests executed
-   issues resolved

## Documentation

-   contracts updated when required

## GitHub

-   issue linked
-   branch used
-   PR created
-   review completed

## Integration

-   develop branch builds successfully
-   existing functionality unaffected

------------------------------------------------------------------------

# 12. Final Demo Responsibilities

## Munshif

Demonstrates:

-   authentication
-   security
-   administration

## Vimaltan

Demonstrates:

-   Job Seeker journey
-   profile
-   CV
-   applications

## Rasadh

Demonstrates:

-   Employer journey
-   vacancies
-   applicant workflow

## Abisegha

Demonstrates:

-   matching
-   application status
-   contact workflow

------------------------------------------------------------------------

# 13. Freeze Status

Document:

HireSync-Team-Development-Plan-v2.0-FROZEN.md

Status:

FROZEN

Role:

Official implementation and ownership plan.
