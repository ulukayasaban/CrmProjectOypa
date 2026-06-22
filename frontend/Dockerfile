# syntax=docker/dockerfile:1
# Multi-stage build for the OYPA CRM frontend (Vite + React + TS).
# Build context is the frontend/ directory (see docker-compose.yml).

# ---- Build stage ----
FROM node:22-alpine AS build
WORKDIR /app

# VITE_* values must be present at BUILD time (Vite inlines them).
ARG VITE_API_BASE_URL=http://localhost:5080/api
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL

# Install dependencies from the lockfile for reproducible builds.
COPY package.json package-lock.json ./
RUN npm ci

# Copy sources and produce the static bundle into /app/dist.
COPY . .
RUN npm run build

# ---- Runtime stage (static serve via Nginx) ----
FROM nginx:alpine AS final

# SPA-aware Nginx config (try_files fallback to index.html).
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Static assets produced by the build stage.
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
