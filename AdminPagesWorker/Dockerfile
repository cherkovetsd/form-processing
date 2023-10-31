FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["AdminPagesWorker/AdminPagesWorker.csproj", "AdminPagesWorker/"]
COPY ["AdminSideServices/AdminSideServices.csproj", "AdminSideServices/"]
COPY ["AdminPagesViews/AdminPagesViews.csproj", "AdminPagesViews/"]
COPY ["Data/Data.csproj", "Data/"]
COPY ["DatabaseInfo/DatabaseInfo.csproj", "DatabaseInfo/"]
COPY ["Utilities/Utilities.csproj", "Utilities/"]
RUN dotnet restore "AdminPagesWorker/AdminPagesWorker.csproj"
COPY . .
WORKDIR "/src/AdminPagesWorker"
RUN dotnet build "AdminPagesWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdminPagesWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "AdminPagesWorker.dll"]
