import {
  Pipe,
  PipeTransform,
} from '@angular/core';

@Pipe({
  name: 'initials',
  standalone: true,
})
export class InitialsPipe
  implements PipeTransform {
  transform(
    value: string | null | undefined,
  ): string {
    const parts =
      (value ?? '')
        .trim()
        .split(/\s+/)
        .filter(Boolean);

    if (parts.length === 0) {
      return '?';
    }

    return parts
      .slice(0, 2)
      .map(
        (part) =>
          part
            .charAt(0)
            .toUpperCase(),
      )
      .join('');
  }
}