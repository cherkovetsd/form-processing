FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["FormStateWorker/FormStateWorker.csproj", "FormStateWorker/"]
COPY ["AdminSideServices/AdminSideServices.csproj", "AdminSideServices/"]
COPY ["AdminPagesViews/AdminPagesViews.csproj", "AdminPagesViews/"]
COPY ["Data/Data.csproj", "Data/"]
COPY ["DatabaseInfo/DatabaseInfo.csproj", "DatabaseInfo/"]
COPY ["Utilities/Utilities.csproj", "Utilities/"]
RUN dotnet restore "FormStateWorker/FormStateWorker.csproj"
COPY . .
WORKDIR "/src/FormStateWorker"
RUN dotnet build "FormStateWorker.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "FormStateWorker.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "FormStateWorker.dll"]
