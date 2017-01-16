FROM microsoft/aspnetcore:1.1.0
ENTRYPOINT ["dotnet", "ApiRunner.dll"]
ARG source=.
WORKDIR /app
COPY $source .
