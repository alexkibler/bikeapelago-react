# ── Stage 1: Build ──────────────────────────────────────────────────────────
FROM node:24-alpine AS builder
WORKDIR /app

COPY package.json pnpm-lock.yaml pnpm-workspace.yaml ./
RUN npm install -g pnpm
RUN pnpm install

COPY . ./

# In production, the frontend is served by Nginx which proxies /api/* to the
# .NET container on the same Docker network. So VITE_PUBLIC_API_URL is empty
# — all API calls use relative paths that Nginx proxies.
ARG VITE_PUBLIC_API_URL=""
ENV VITE_PUBLIC_API_URL=${VITE_PUBLIC_API_URL}

RUN npm run build

# ── Stage 2: Nginx ───────────────────────────────────────────────────────────
FROM nginx:alpine AS runtime

# Remove the default Nginx config
RUN rm /etc/nginx/conf.d/default.conf

# Copy our custom config template for envsubst
COPY nginx.conf.template /etc/nginx/templates/default.conf.template

# Default value for the proxy pass URL
ENV API_PROXY_URL=http://bikeapelago-api:8080/api/

# Copy the built React app
COPY --from=builder /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
