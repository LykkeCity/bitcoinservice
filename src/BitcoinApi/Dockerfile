FROM microsoft/dotnet:2.1-aspnetcore-runtime
ENTRYPOINT ["dotnet", "BitcoinApi.dll"]
ARG source=.
WORKDIR /app
COPY $source .
