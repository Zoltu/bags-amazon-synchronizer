FROM zoltu/aspnetcore

COPY . /app

WORKDIR /app
RUN dotnet restore

WORKDIR /app/tests
RUN dotnet build
RUN dotnet test

WORKDIR /app/application
RUN dotnet build

ENTRYPOINT ["dotnet", "run"]
