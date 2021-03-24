FROM mcr.microsoft.com/dotnet/core/sdk:2.1

WORKDIR /app

COPY . .

RUN dotnet restore

ENTRYPOINT ["dotnet","run"]

