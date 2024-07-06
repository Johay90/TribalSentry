# TribalSentry

TribalSentry is a comprehensive solution for monitoring Tribal Wars game data, consisting of a .NET Core Web API and a Discord bot. The API fetches and processes data from Tribal Wars game servers, while the Discord bot provides real-time notifications and management of monitoring tasks.

## TribalSentry API

### Features
- Fetch village data, including coordinates, owner, points, and continent
- Retrieve player information with associated tribe details
- Get tribe (ally) data
- Fetch barbarian villages with optional continent filtering
- Support for different Tribal Wars markets (e.g., UK, international)
- Case-insensitive continent matching

### Prerequisites
- .NET Core SDK (version 6.0 or later recommended)

### Getting Started
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
   dotnet run --project TribalSentry.API
   ```
The API should now be running on `https://localhost:7171`.

### API Endpoints
- `GET /api/tribalwars/villages`: Get all villages
- `GET /api/tribalwars/barbarian-villages`: Get barbarian villages
- `GET /api/tribalwars/players`: Get all players
- `GET /api/tribalwars/tribes`: Get all tribes

For full details on query parameters, please refer to the API documentation.

### Example Usage
Fetch barbarian villages in continent K55 for the UK market, world 1:
```
GET https://localhost:7171/api/tribalwars/barbarian-villages?market=tribalwars.co.uk&worldName=uk1&continent=K55
```

## TribalSentry Discord Bot

The TribalSentry Discord Bot provides real-time monitoring and notifications for barbarian villages in Tribal Wars.

### Features
- Monitor barbarian villages across multiple Tribal Wars worlds
- Receive notifications for new barbarian villages and conquered villages
- Manage monitors using Discord slash commands
- View active monitors and their settings

### Prerequisites
- .NET Core SDK (version 6.0 or later recommended)
- A Discord bot token (obtain from the Discord Developer Portal)

### Getting Started
1. Set up your Discord bot token in the `appsettings.json` file:
   ```json
   {
     "Discord": {
       "BotToken": "YOUR_BOT_TOKEN_HERE"
     }
   }
   ```
2. Run the Discord bot:
   ```
   dotnet run --project TribalSentry.Bot
   ```

### Bot Commands
- `/addmonitor`: Add a new monitor for barbarian villages
- `/updatemonitor`: Update an existing monitor
- `/removemonitor`: Remove an existing monitor
- `/viewmonitors`: View all active monitors

For detailed usage of each command, use the Discord slash command interface.

## Contributing
Contributions are welcome! Please feel free to submit a Pull Request.

## License
This project is licensed under the MIT License - see the [LICENSE.md](LICENSE.md) file for details.
