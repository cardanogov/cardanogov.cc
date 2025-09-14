const PROXY_HOST = 'https://api.koios.rest';

const PROXY_CONFIG = [
  {
    context: [
      '/api'
    ],
    target: PROXY_HOST,
    secure: false
  }
];

module.exports = PROXY_CONFIG;
