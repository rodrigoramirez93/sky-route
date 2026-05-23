export type CabinClass = 'Economy' | 'Business' | 'First';

export const CABIN_CLASSES: ReadonlyArray<CabinClass> = ['Economy', 'Business', 'First'];

// Human-readable labels shown in the UI. Kept separate from the type/value
// mapping so wire contracts stay stable while we honour the Gherkin wording
// (e.g. "First Class" rather than the internal "First").
export const CABIN_CLASS_LABEL: Record<CabinClass, string> = {
  Economy: 'Economy',
  Business: 'Business',
  First: 'First Class',
};

// Backend uses numeric enum values (1=Economy, 2=Business, 3=First).
export const CABIN_CLASS_VALUE: Record<CabinClass, number> = {
  Economy: 1,
  Business: 2,
  First: 3,
};
