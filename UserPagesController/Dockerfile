FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443
ENV ASPNETCORE_URLS=https://*:443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["UserPagesController/UserPagesController.csproj", "UserPagesController/"]
COPY ["DatabaseInfo/DatabaseInfo.csproj", "DatabaseInfo/"]
COPY ["Data/Data.csproj", "Data/"]
COPY ["UserSideServices/UserSideServices.csproj", "UserSideServices/"]
COPY ["UserPagesViews/UserPagesViews.csproj", "UserPagesViews/"]
COPY ["Utilities/Utilities.csproj", "Utilities/"]
RUN dotnet restore "UserPagesController/UserPagesController.csproj"
COPY . .
WORKDIR "/src/UserPagesController"
RUN sed -i '4 i <AssemblyName>app</AssemblyName>' UserPagesController.csproj
RUN dotnet build "UserPagesController.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "UserPagesController.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "app.dll", "--server.urls", "https://+:443"]
