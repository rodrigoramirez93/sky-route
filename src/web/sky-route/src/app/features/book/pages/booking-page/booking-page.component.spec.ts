import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideBookFeature } from '../../book.providers';
import { BookingPageComponent } from './booking-page.component';

describe('BookingPageComponent', () => {
  it('creates', async () => {
    await TestBed.configureTestingModule({
      imports: [BookingPageComponent],
      providers: [
        provideRouter([]),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideBookFeature({ apiBaseUrl: 'http://test' }),
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(BookingPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
