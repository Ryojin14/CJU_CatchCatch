FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 프로젝트 파일 복사 및 패키지 복원
COPY ["CJUCatch.Server/CJUCatch.Server.csproj", "CJUCatch.Server/"]
COPY ["CJUCatch.Shared/CJUCatch.Shared.csproj", "CJUCatch.Shared/"]
RUN dotnet restore "CJUCatch.Server/CJUCatch.Server.csproj"

# 나머지 코드 복사 및 빌드
COPY . .
WORKDIR "/src/CJUCatch.Server"
RUN dotnet publish "CJUCatch.Server.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 실행용 경량 이미지
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
COPY --from=build /app/publish .

# Render 기본 포트 환경변수 설정
EXPOSE 8080
ENV ASPNETCORE_HTTP_PORTS=8080

ENTRYPOINT ["dotnet", "CJUCatch.Server.dll"]
