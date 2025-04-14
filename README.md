# Student Tracking System

Isang web application para sa real-time na tracking ng mga estudyante gamit ang C# at Leaflet maps.

## Features

- Student at Parent authentication
- Student-Parent connection system
- Real-time location tracking gamit ang Leaflet maps
- SignalR para sa real-time updates
- Secure na JWT authentication

## Prerequisites

- .NET 6.0 SDK
- Node.js at npm
- SQL Server
- Visual Studio 2022 o Visual Studio Code

## Installation

1. I-clone ang repository:
```bash
git clone <repository-url>
```

2. I-restore ang .NET dependencies:
```bash
dotnet restore
```

3. I-install ang client-side dependencies:
```bash
cd ClientApp
npm install
```

4. I-update ang connection string sa `appsettings.json`

5. I-run ang database migrations:
```bash
dotnet ef database update
```

## Development

Para i-run ang application sa development mode:

1. I-run ang backend:
```bash
dotnet run
```

2. I-run ang frontend:
```bash
cd ClientApp
npm start
```

## Architecture

### Backend (C#)
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server database
- SignalR para sa real-time communication
- JWT Authentication

### Frontend (React)
- React with TypeScript
- React Router para sa navigation
- Leaflet para sa maps
- SignalR client para sa real-time updates
- Axios para sa HTTP requests

### Database Tables
- Users
- StudentParentConnection
- LocationTracking
- TrackingSession

## Security Features

- Secure na password hashing gamit ang BCrypt
- JWT authentication para sa API requests
- Role-based access control
- Secure na connection handling

## API Endpoints

### Authentication
- POST /api/auth/register
- POST /api/auth/login

### Connection
- POST /api/connection/request
- POST /api/connection/respond/{connectionId}
- GET /api/connection/pending

### Tracking
- POST /api/tracking/start
- POST /api/tracking/stop
- GET /api/tracking/status/{studentId}

## Contributing

Para sa mga contributors, please follow ang coding standards at testing requirements bago mag-submit ng pull request. 