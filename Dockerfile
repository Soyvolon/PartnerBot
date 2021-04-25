#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:5.0 AS base
WORKDIR /app
# If we do move to a web client/api, we will need these later.
#EXPOSE 80
#EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["PartnerBot/PartnerBot.csproj", "PartnerBot/"]
RUN ls && dotnet restore "PartnerBot/PartnerBot.csproj" --configfile "NuGet.config"
COPY . .
WORKDIR "/src/PartnerBot"
RUN dotnet build "PartnerBot.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PartnerBot.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "PartnerBot.dll"]
