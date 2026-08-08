/**
 * Production environment. In the Docker deployment the nginx container serves the
 * built application and proxies /api to the API service, so requests are same-origin.
 */
export const environment = {
  production: true,
  apiUrl: '',
};
