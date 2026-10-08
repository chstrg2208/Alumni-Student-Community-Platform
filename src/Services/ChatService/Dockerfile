FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["AlumniConnect.slnx", "./"]
COPY ["src/Services/ChatService/ChatService.csproj", "src/Services/ChatService/"]
RUN dotnet restore "src/Services/ChatService/ChatService.csproj"

COPY src/Services/ChatService/ src/Services/ChatService/
WORKDIR "/src/src/Services/ChatService"
RUN dotnet build "ChatService.csproj" -c Release -o /app/build
RUN dotnet publish "ChatService.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .
EXPOSE 5199
ENV ASPNETCORE_URLS=http://+:5199
ENTRYPOINT ["dotnet", "ChatService.dll"]
