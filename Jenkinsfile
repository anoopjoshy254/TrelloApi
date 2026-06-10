pipeline {
 
    agent any
 
    environment {
        IMAGE = "trello-api:${BUILD_NUMBER}"
        NETWORK = "trello-net"
        MYSQL_CONT = "trello-mysql"
        API_CONT = "trello-api"
 
        MYSQL_PWD = "root"
        MYSQL_DB = "trello_db"
    }
 
    stages {
 
        stage('Checkout') {
            steps {
                checkout scm
            }
        }
 
        stage('Build Docker Image') {
            steps {
                bat "docker build -t %IMAGE% ."
            }
        }
 
        stage('Create Network') {
            steps {
                bat "docker network inspect %NETWORK% >nul 2>&1 || docker network create %NETWORK%"
            }
        }
 
        stage('Start MySQL') {
            steps {
                bat """
                docker rm -f %MYSQL_CONT% 2>nul
 
                docker run -d --name %MYSQL_CONT% --network %NETWORK% ^
                    -e MYSQL_ROOT_PASSWORD=%MYSQL_PWD% ^
                    -e MYSQL_DATABASE=%MYSQL_DB% ^
                    -p 3307:3306 ^
                    -v mysql-data:/var/lib/mysql ^
                    mysql:8.0
                """
            }
        }
 
        stage('Wait for MySQL (HEALTHCHECK equivalent)') {
            steps {
                bat """
                echo Waiting for MySQL to be ready...
 
                :loop
                docker exec %MYSQL_CONT% mysqladmin ping -h localhost -uroot -p%MYSQL_PWD% >nul 2>&1
 
                IF ERRORLEVEL 1 (
                    timeout /t 5 >nul
                    goto loop
                )
 
                echo MySQL is ready!
                """
            }
        }
 
        stage('Run API') {
            steps {
                bat """
                docker rm -f %API_CONT% 2>nul
 
                docker run -d --name %API_CONT% --network %NETWORK% ^
                    -e ASPNETCORE_ENVIRONMENT=Development ^
                    -e ASPNETCORE_URLS=http://+:8080 ^
                    -e MYSQL_CONNECTION_STRING="Server=%MYSQL_CONT%;Port=3306;Database=%MYSQL_DB%;User=root;Password=%MYSQL_PWD%;" ^
                    -e JWT_ISSUER=trello-api ^
                    -e JWT_AUDIENCE=trello-clone ^
                    -e JWT_SECRET=change-this-development-secret-at-least-32-characters ^
                    -e JWT_ACCESS_TOKEN_MINUTES=60 ^
                    -e JWT_REFRESH_TOKEN_DAYS=30 ^
                    -e MEDIA_BASE_URL=/media ^
                    -p 5095:8080 ^
                    -v api-media:/app/SimpleStorage ^
                    %IMAGE%
                """
            }
        }
 
    }
}
 
 