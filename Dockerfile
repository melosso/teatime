# syntax=docker/dockerfile:1
FROM mcr.microsoft.com/dotnet/sdk:11.0-preview AS build
WORKDIR /src

# Restore against the Teatime build/package props and the project.
COPY Teatime/global.json Teatime/Directory.Build.props Teatime/Directory.Packages.props Teatime/
COPY Teatime/src/Teatime/Teatime.csproj Teatime/src/Teatime/
RUN cd Teatime && dotnet restore src/Teatime/Teatime.csproj

# Bring in the source and your blog content, then publish.
COPY Teatime/src/Teatime/ Teatime/src/Teatime/
COPY content/ content/
RUN cd Teatime && dotnet publish src/Teatime/Teatime.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:11.0-preview AS runtime
WORKDIR /app
COPY --from=build /app ./

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "Teatime.dll"]
