import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideHttpClientTesting } from '@angular/common/http/testing';
import { provideSearchFeature } from '../../search.providers';
import { SearchFormComponent } from './search-form.component';

describe('SearchFormComponent', () => {
  it('creates with an invalid initial form (missing origin/destination)', async () => {
    await TestBed.configureTestingModule({
      imports: [SearchFormComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideSearchFeature({ apiBaseUrl: 'http://test' }),
      ],
    }).compileComponents();
    const fixture = TestBed.createComponent(SearchFormComponent);
    fixture.detectChanges();
    expect(fixture.componentInstance.form.valid).toBe(false);
  });
});
