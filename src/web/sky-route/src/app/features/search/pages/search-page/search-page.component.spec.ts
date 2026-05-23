import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideSearchFeature } from '../../search.providers';
import { SearchPageComponent } from './search-page.component';

describe('SearchPageComponent', () => {
  it('creates', async () => {
    await TestBed.configureTestingModule({
      imports: [SearchPageComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideSearchFeature({ apiBaseUrl: 'http://test' }),
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(SearchPageComponent);
    expect(fixture.componentInstance).toBeTruthy();
  });
});
