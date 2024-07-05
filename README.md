# TribalSentry API

TribalSentry API is a .NET Core Web API project designed to fetch and process data from Tribal Wars game servers. It provides endpoints for retrieving information about villages, players, tribes, and barbarian villages across different game worlds and markets.

## Features

- Fetch village data, including coordinates, owner, points, and continent
- Retrieve player information with associated tribe details
- Get tribe (ally) data
- Fetch barbarian villages with optional continent filtering
- Support for different Tribal Wars markets (e.g., UK, international)
- Xontinent matching

## Prerequisites

- .NET Core SDK (version 6.0 or later recommended)

## Getting Started

1. Clone the repository:
   ```
   git clone https://github.com/Johay90/TribalSentry.git
   ```

2. Navigate to the project directory:
   ```
   cd TribalSentry
   ```

3. Restore the NuGet packages:
   ```
   dotnet restore
   ```

4. Run the application:
   ```
   dotnet run
   ```

The API should now be running on `https://localhost:5001` (or `http://localhost:5000`).

## API Endpoints

- `GET /api/tribalwars/villages`: Get all villages
  - Query parameters:
    - `market`: The Tribal Wars market (e.g., "tribalwars.co.uk")
    - `worldName`: The world name (e.g., "uk1")
    - `minPoints` (optional): Minimum village points
    - `maxPoints` (optional): Maximum village points

- `GET /api/tribalwars/barbarian-villages`: Get barbarian villages
  - Query parameters:
    - `market`: The Tribal Wars market
    - `worldName`: The world name
    - `continent` (optional): Filter by continent (e.g., "K55")

- `GET /api/tribalwars/players`: Get all players
  - Query parameters:
    - `market`: The Tribal Wars market
    - `worldName`: The world name

- `GET /api/tribalwars/tribes`: Get all tribes
  - Query parameters:
    - `market`: The Tribal Wars market
    - `worldName`: The world name

## Example Usage

Fetch barbarian villages in continent K55 for the UK market, world 1:

```
GET https://localhost:5001/api/tribalwars/barbarian-villages?market=tribalwars.co.uk&worldName=uk1&continent=K55
```

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.
