# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

# Restore against the root build/package props and the project.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Teatime/Teatime.csproj src/Teatime/
RUN dotnet restore src/Teatime/Teatime.csproj

# Bring in the source and your blog content, then publish.
COPY src/Teatime/ src/Teatime/
COPY content/ content/
RUN dotnet publish src/Teatime/Teatime.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Teatime.dll"]
