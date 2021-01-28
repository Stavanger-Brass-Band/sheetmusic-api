FROM microsoft/dotnet:2.2-aspnetcore-runtime AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM microsoft/dotnet:2.2-sdk AS build
WORKDIR /src
COPY ["SheetMusic.Api/SheetMusic.Api.csproj", "SheetMusic.Api/"]
RUN dotnet restore "SheetMusic.Api/SheetMusic.Api.csproj"
COPY . .
WORKDIR "/src/SheetMusic.Api"
RUN dotnet build "SheetMusic.Api.csproj" -c Release -o /app

FROM build AS publish
RUN dotnet publish "SheetMusic.Api.csproj" -c Release -o /app

FROM base AS final
WORKDIR /app
COPY --from=publish /app .
ENTRYPOINT ["dotnet", "SheetMusic.Api.dll"]