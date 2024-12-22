# **InstaShare**

InstaShare is a distributed application designed for efficient and secure file sharing and management. It leverages a microservices architecture, asynchronous communication with RabbitMQ, advanced file compression using IronZip, and a distributed file system based on Docker containers.

Each microservice is fully containerized and communicates through an API Gateway configured with Ocelot.

---

## **Table of Contents**

1. [Project Overview](#project-overview)
2. [Features](#features)
3. [Architecture](#architecture)
4. [Technologies Used](#technologies-used)
5. [Prerequisites](#prerequisites)
6. [Environment Configuration](#environment-configuration)
7. [Usage](#usage)
8. [Contributing](#contributing)
9. [License](#license)

---

## **Project Overview**

InstaShare allows users to:

- Upload and download files.
- Manage files (rename, delete).
- Compress files automatically during upload.
- Authenticate securely using JWT.
- Utilize a distributed system for file storage and management.

Uploaded files are **not stored in the database**. Instead:

- File metadata (name, size, and location) is saved in a SQL Server database.
- Files are stored in a Docker container with a shared volume, acting as a distributed file system.

---

## **Features**

- **Containerized Microservices**: All services, including SQL Server and RabbitMQ, run in Docker containers.
- **Asynchronous Communication with RabbitMQ**: Efficient handling of file upload and download processes.
- **File Compression with IronZip**: Files are compressed automatically before being stored.
- **Centralized Routing**: API Gateway with Ocelot handles request routing.
- **Secure Authentication**: JWT is used to protect sensitive routes.
- **Distributed File System**: Files are stored in a shared volume inside a Docker container.

---

## **Architecture**

### **Simplified Diagram**

plaintext

Copy code

`[Frontend] <---> [API Gateway (Ocelot)] <---> [Auth Service]                                    |        <---> [File Upload Service]                                    |        <---> [File Management Service]                                    |        <---> [RabbitMQ]                                    |        <---> [SQL Server]                                    |        <---> [File System (Shared Volume)]`

### **Microservices**

1. **Auth Service**:
    
    - User registration, login, and JWT-based authentication.
    - Sensitive operations like password changes and account deletion.
2. **File Upload Service**:
    
    - Handles file uploads and compresses them using IronZip.
    - Publishes asynchronous notifications via RabbitMQ.
3. **File Management Service**:
    
    - Supports file downloads, renaming, and deletion.
    - Stores metadata in SQL Server.
4. **API Gateway**:
    
    - Centralized routing using Ocelot.
    - Protects routes with JWT-based authentication.
5. **Distributed File System**:
    
    - Stores files in a Docker container with a shared volume.
6. **RabbitMQ**:
    
    - Enables asynchronous messaging for file-related events.

---

## **Technologies Used**

- **Backend**:
    - ASP.NET Core 8.0
    - Entity Framework Core for database management
    - Ocelot for API Gateway
    - IronZip for file compression
- **Database**:
    - SQL Server (container `sql_server`)
- **Messaging**:
    - RabbitMQ (container `rabbitmq`)
- **Containerization**:
    - Docker and Docker Compose

---

## **Prerequisites**

Before setting up the project, ensure you have the following installed:

1. Docker Desktop
2. [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0)

---

## **Environment Configuration**

### **Sensitive Credentials**

While this example uses the `appsettings.json` file to store the secure key and other credentials, **it is strongly recommended to use a secret management system** such as **Docker Secrets** for production environments. For example:

1. Create a Docker secret:
    
    bash
    
    Copy code
    
    `echo "your_secure_key" | docker secret create jwt_secret_key -`
    
2. Update the `docker-compose.yml` file to use the secret:
    
    yaml
    
    Copy code
    
    `services:   instashareauthservice:     secrets:       - jwt_secret_key secrets:   jwt_secret_key:     external: true`
    
3. Update the service configuration to read the secret from a file (e.g., `/run/secrets/jwt_secret_key`).
    

---

### **Environment Variables**

Ensure proper configuration for the microservices to connect with SQL Server and RabbitMQ:

1. **SQL Server**:
    
    yaml
    
    Copy code
    
    `environment:   - SA_PASSWORD=Admin123*   - ACCEPT_EULA=Y`
    
2. **RabbitMQ**:
    
    yaml
    
    Copy code
    
    `environment:   - RABBITMQ_DEFAULT_USER=guest   - RABBITMQ_DEFAULT_PASS=guest`
    
3. **Connection Strings**:
    
    yaml
    
    Copy code
    
    `environment:   - ConnectionStrings__DefaultConnection=Server=sql_server;Database=InstaShare;User Id=sa;Password=Admin123*;Encrypt=false;   - RabbitMQ__Host=rabbitmq`
    

---

### **Starting the Containers**

1. Clone the repository:
    
    bash
    
    Copy code
    
    `git clone https://github.com/your-username/instashare.git cd instashare`
    
2. Build and start the containers:
    
    bash
    
    Copy code
    
    `docker-compose up --build`
    
3. Access the API Gateway:
    
    plaintext
    
    Copy code
    
    `http://localhost:5000`
    

---

## **Usage**

### **Main Routes**

1. **Authentication** (`/api/auth`):
    
    - `POST /register`: Register a new user.
    - `POST /login`: Log in and receive a JWT token.
2. **File Management** (`/api/files`):
    
    - `GET /`: List all files for the authenticated user.
    - `GET /downloadfile/{fileName}`: Download a file by name.
    - `PUT /renamefile`: Rename a file.
    - `DELETE /deletefile`: Delete a file.
3. **File Upload** (`/api/upload`):
    
    - `POST /uploadfile`: Upload a file (automatically compressed).

---

## **Contributing**

We welcome contributions! Follow these steps:

1. Fork the repository.
2. Create a feature branch:
    
    bash
    
    Copy code
    
    `git checkout -b my-feature`
    
3. Commit your changes.
4. Submit a pull request.

---

## **License**

This project is licensed under the MIT License.
