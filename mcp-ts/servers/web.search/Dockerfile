FROM node:20-alpine AS build
WORKDIR /app
COPY package.json tsconfig.json .
COPY src ./src
RUN corepack enable
RUN pnpm install
RUN pnpm build

FROM node:20-alpine
WORKDIR /app
ENV NODE_ENV=production
COPY --from=build /app/dist ./dist
COPY package.json .
RUN corepack enable
RUN pnpm install --prod
EXPOSE 8081
CMD ["node", "dist/index.js"]
