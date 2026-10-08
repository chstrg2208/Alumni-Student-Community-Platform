FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AlumniConnect.slnx", "./"]
COPY ["src/Services/UserService/UserService.csproj", "src/Services/UserService/"]
RUN dotnet restore "src/Services/UserService/UserService.csproj"

COPY src/Services/UserService/ src/Services/UserService/
WORKDIR "/src/src/Services/UserService"
RUN dotnet build "UserService.csproj" -c Release -o /app/build
RUN dotnet publish "UserService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5164
ENV ASPNETCORE_URLS=http://+:5164
ENTRYPOINT ["dotnet", "UserService.dll"]
