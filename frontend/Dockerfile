# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM node:24-alpine AS builder
WORKDIR /app

RUN npm install -g pnpm

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
COPY packages ./packages
RUN pnpm install

# In production, the frontend is served by Nginx which proxies /api/* to the
# .NET container on the same Docker network. So VITE_PUBLIC_API_URL is empty
# — all API calls use relative paths that Nginx proxies.
ARG VITE_PUBLIC_API_URL=""
ENV VITE_PUBLIC_API_URL=${VITE_PUBLIC_API_URL}

RUN pnpm run build

# ── Runtime Template (shared logic) ────────────────────────────────────────────
FROM nginx:alpine AS runtime-template

RUN rm /etc/nginx/conf.d/default.conf
ENV API_PROXY_URL=http://bikeapelago-api:8080/api/ \
    API_HUBS_URL=http://bikeapelago-api:8080/hubs/
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]

# ── App Frontend (bikepelago-app) ───────────────────────────────────────────────
FROM runtime-template AS runtime-frontend
COPY --from=builder /app/packages/apps/bikepelago-app/deploy/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=builder /app/packages/apps/bikepelago-app/dist /usr/share/nginx/html

# ── Admin Frontend (admin-ui) ───────────────────────────────────────────────────
FROM runtime-template AS runtime-admin-ui
COPY --from=builder /app/packages/apps/admin-ui/deploy/nginx.conf.template /etc/nginx/templates/default.conf.template
COPY --from=builder /app/packages/apps/admin-ui/dist /usr/share/nginx/html
