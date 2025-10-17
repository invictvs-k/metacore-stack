# syntax=docker/dockerfile:1
FROM node:20-alpine
WORKDIR /app
COPY package.json ./
RUN corepack enable && pnpm install
COPY . .
EXPOSE 8081
CMD ["pnpm", "dev"]
