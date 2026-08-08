# syntax=docker/dockerfile:1
# Multi-stage build for the Angular frontend.
#
# Stage 1: install dependencies and build the production bundle with the Angular CLI.
# Stage 2: serve the static bundle from nginx; the Angular dev server is never used in
# production. nginx also proxies /api to the API service (see frontend/nginx.conf).

FROM node:24-alpine AS build
WORKDIR /app

# Install from the lockfile first so the layer is cached until dependencies change.
COPY frontend/assignment-management-ui/package.json frontend/assignment-management-ui/package-lock.json ./
RUN npm ci

# Copy the whole app and build the production bundle (environment.prod.ts is used,
# so API calls are same-origin and hit the nginx /api proxy).
COPY frontend/assignment-management-ui/ ./
RUN npm run build

FROM nginx:1.27-alpine AS serve
COPY docker/frontend/nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist/assignment-management-ui/browser /usr/share/nginx/html

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
