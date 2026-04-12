# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM node:24-alpine AS builder
WORKDIR /app

RUN npm install -g pnpm

COPY frontend/package.json frontend/pnpm-lock.yaml frontend/pnpm-workspace.yaml ./
COPY frontend/packages ./packages
RUN pnpm install

# In production, the frontend is served by Nginx which proxies /api/* to the
# .NET container on the same Docker network. So VITE_PUBLIC_API_URL is empty
# — all API calls use relative paths that Nginx proxies.
ARG VITE_PUBLIC_API_URL=""
ENV VITE_PUBLIC_API_URL=${VITE_PUBLIC_API_URL}

RUN pnpm run build

# ── Runtime (parameterized for any app) ──────────────────────────────────────
FROM nginx:alpine AS runtime

ARG TARGET_APP=bikepelago-app

# Remove the default Nginx config
RUN rm /etc/nginx/conf.d/default.conf

# Copy our custom config template for envsubst
COPY --from=builder /app/packages/apps/${TARGET_APP}/deploy/nginx.conf.template /etc/nginx/templates/default.conf.template

# Default values for API proxy URLs
ENV API_PROXY_URL=http://bikeapelago-api:8080/api/ \
    API_HUBS_URL=http://bikeapelago-api:8080/hubs/

# Copy the built React app
COPY --from=builder /app/packages/apps/${TARGET_APP}/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
