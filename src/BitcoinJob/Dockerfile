FROM microsoft/aspnetcore:2.0
ENTRYPOINT ["dotnet", "BitcoinJob.dll"]
ARG source=.
WORKDIR /app
COPY $source .
