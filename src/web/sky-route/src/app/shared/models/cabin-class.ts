export type CabinClass = 'Economy' | 'Business' | 'First';

export const CABIN_CLASSES: ReadonlyArray<CabinClass> = ['Economy', 'Business', 'First'];

// Backend uses numeric enum values (1=Economy, 2=Business, 3=First).
export const CABIN_CLASS_VALUE: Record<CabinClass, number> = {
  Economy: 1,
  Business: 2,
  First: 3,
};
