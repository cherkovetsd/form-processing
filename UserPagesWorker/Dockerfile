FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["UserPagesWorker/UserPagesWorker.csproj", "UserPagesWorker/"]
COPY ["Data/Data.csproj", "Data/"]
COPY ["UserSideServices/UserSideServices.csproj", "UserSideServices/"]
COPY ["DatabaseInfo/DatabaseInfo.csproj", "DatabaseInfo/"]
COPY ["UserPagesViews/UserPagesViews.csproj", "UserPagesViews/"]
COPY ["Utilities/Utilities.csproj", "Utilities/"]
RUN dotnet restore "UserPagesWorker/UserPagesWorker.csproj"
COPY . .
WORKDIR "/src/UserPagesWorker"
RUN dotnet build "UserPagesWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserPagesWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "UserPagesWorker.dll"]
