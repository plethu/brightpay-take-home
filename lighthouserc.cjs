const baseUrl = process.env.LHCI_BASE_URL || 'http://checkout-web:8080';
const routeUrl = process.env.LHCI_URL || `${baseUrl.replace(/\/$/, '')}/cart`;

module.exports = {
  ci: {
    collect: {
      url: [routeUrl],
      numberOfRuns: 3,
      settings: {
        preset: 'desktop',
        onlyCategories: ['performance', 'accessibility', 'best-practices', 'seo'],
        skipAudits: ['is-on-https', 'redirects-http'],
        chromeFlags: '--headless=new --no-sandbox --disable-dev-shm-usage',
      },
    },
    assert: {
      assertions: {
        'categories:performance': ['error', { minScore: 0.95 }],
        'categories:accessibility': ['error', { minScore: 0.95 }],
        'categories:best-practices': ['warn', { minScore: 0.9 }],
        'categories:seo': ['warn', { minScore: 0.9 }],
        'first-contentful-paint': ['error', { maxNumericValue: 1500, aggregationMethod: 'median' }],
        'largest-contentful-paint': ['error', { maxNumericValue: 2000, aggregationMethod: 'median' }],
        'total-byte-weight': ['error', { maxNumericValue: 250000, aggregationMethod: 'median' }],
      },
    },
    upload: {
      target: 'filesystem',
      outputDir: 'artifacts/lighthouse',
    },
  },
};
