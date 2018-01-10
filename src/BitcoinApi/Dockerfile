FROM microsoft/aspnetcore:2.0
ENTRYPOINT ["dotnet", "BitcoinApi.dll"]
ARG source=.
WORKDIR /app
COPY $source .
