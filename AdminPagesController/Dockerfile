﻿FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 443
ENV ASPNETCORE_URLS=https://*:443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["AdminPagesController/AdminPagesController.csproj", "AdminPagesController/"]
COPY ["AdminSideServices/AdminSideServices.csproj", "AdminSideServices/"]
COPY ["AdminPagesViews/AdminPagesViews.csproj", "AdminPagesViews/"]
COPY ["Data/Data.csproj", "Data/"]
COPY ["DatabaseInfo/DatabaseInfo.csproj", "DatabaseInfo/"]
COPY ["Utilities/Utilities.csproj", "Utilities/"]
RUN dotnet restore "AdminPagesController/AdminPagesController.csproj"
COPY . .
WORKDIR "/src/AdminPagesController"
RUN sed -i '4 i <AssemblyName>app</AssemblyName>' AdminPagesController.csproj
RUN dotnet build "AdminPagesController.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "AdminPagesController.csproj" -c Release -o /app/publish -p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "app.dll", "--server.urls", "https://+:443"]
