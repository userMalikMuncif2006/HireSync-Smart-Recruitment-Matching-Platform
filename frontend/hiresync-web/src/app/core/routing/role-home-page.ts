import { Component, inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-role-home-page',
  template: `
    <main class="role-home">
      <section>
        <p>HireSync</p>
        <h1>{{ title }}</h1>
        <p>{{ message }}</p>
      </section>
    </main>
  `,
  styles: [`
    :host {
      display: block;
      min-height: 100dvh;
    }

    .role-home {
      min-height: 100dvh;
      display: grid;
      place-items: center;
      padding: 1.5rem;
      box-sizing: border-box;
      background: #f7faf9;
      color: #10231f;
    }

    section {
      width: min(100%, 36rem);
      padding: 2rem;
      border: 1px solid #dbe7e3;
      border-radius: 1rem;
      background: #ffffff;
    }

    section > p:first-child {
      color: #0f766e;
      font-weight: 700;
    }

    h1 {
      margin: 0.5rem 0;
    }

    section > p:last-child {
      color: #52645f;
    }
  `],
})
export class RoleHomePage {
  private readonly route = inject(ActivatedRoute);

  readonly title =
    this.route.snapshot.data['title'] as string;

  readonly message =
    this.route.snapshot.data['message'] as string;
}