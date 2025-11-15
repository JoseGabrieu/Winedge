FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.sln .
COPY */*.csproj ./
RUN for f in *.csproj; do mkdir -p ${f%.*} && mv $f ${f%.*}; done

RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publish \
    -p:AssemblyName=Winedge

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 5000
ENV ASPNETCORE_URLS=http://+:5000

ENTRYPOINT ["dotnet", "Winedge.dll"]