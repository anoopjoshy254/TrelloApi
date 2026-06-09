FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["TrelloApi.csproj", "./"]
RUN dotnet restore "TrelloApi.csproj"
COPY . .
RUN dotnet build "TrelloApi.csproj" -c Release -o /app/build
RUN dotnet publish "TrelloApi.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "TrelloApi.dll"]
