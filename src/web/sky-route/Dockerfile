FROM node:22-alpine AS build
WORKDIR /app

# Install dependencies first for better layer caching
COPY package*.json ./
RUN npm ci

# Copy source and build for production (uses environment.prod.ts)
COPY . .
RUN npm run build -- --configuration development

FROM nginx:alpine AS runtime
COPY --from=build /app/dist/sky-route/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
