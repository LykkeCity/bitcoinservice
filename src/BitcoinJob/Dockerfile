FROM microsoft/dotnet:2.1-aspnetcore-runtime
ENTRYPOINT ["dotnet", "BitcoinJob.dll"]
ARG source=.
WORKDIR /app
COPY $source .
