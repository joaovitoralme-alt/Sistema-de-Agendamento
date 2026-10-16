# Monta a aplicação inteira (site Vue + API .NET) numa imagem só.
# Usado pelo docker-compose.prod.yml. Rode a partir da pasta SiteNenem.

# 1) Compila o front-end. O vite.config.js manda o resultado para Backend/src/Clinica.Api/wwwroot.
FROM node:22-alpine AS front
WORKDIR /src/Frontend
COPY Frontend/package.json Frontend/package-lock.json ./
RUN npm ci
COPY Frontend/ ./
RUN npm run build

# 2) Compila e publica a API, já com o site dentro do wwwroot.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS back
WORKDIR /src
COPY Backend/ Backend/
COPY --from=front /src/Backend/src/Clinica.Api/wwwroot Backend/src/Clinica.Api/wwwroot
RUN dotnet publish Backend/src/Clinica.Api -c Release -o /app

# 3) Imagem final, só com o necessário para rodar.
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=back /app .
ENV ASPNETCORE_URLS=http://+:8080 \
    Fotos__Pasta=/dados/fotos
EXPOSE 8080
ENTRYPOINT ["dotnet", "Clinica.Api.dll"]
