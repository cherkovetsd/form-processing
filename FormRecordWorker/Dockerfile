FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["FormRecordWorker/FormRecordWorker.csproj", "FormRecordWorker/"]
COPY ["Data/Data.csproj", "Data/"]
COPY ["UserSideServices/UserSideServices.csproj", "UserSideServices/"]
COPY ["DatabaseInfo/DatabaseInfo.csproj", "DatabaseInfo/"]
COPY ["UserPagesViews/UserPagesViews.csproj", "UserPagesViews/"]
COPY ["Utilities/Utilities.csproj", "Utilities/"]
RUN dotnet restore "FormRecordWorker/FormRecordWorker.csproj"
COPY . .
WORKDIR "/src/FormRecordWorker"
RUN dotnet build "FormRecordWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FormRecordWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FormRecordWorker.dll"]
