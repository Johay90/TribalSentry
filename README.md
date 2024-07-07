# TribalSentry

TribalSentry is a comprehensive solution for monitoring Tribal Wars game data, consisting of a .NET Core Web API and a Discord bot. The API fetches and processes data from Tribal Wars game servers, while the Discord bot provides real-time notifications and management of monitoring tasks for barbarian villages.

## Table of Contents

- [TribalSentry API](#tribalsentry-api)
- [TribalSentry Discord Bot](#tribalsentry-discord-bot)
- [Installation](#installation)
- [Usage](#usage)
- [Contributing](#contributing)
- [License](#license)

## TribalSentry API

### Features

- Fetch village data, including coordinates, owner, points, and continent
- Retrieve player information with associated tribe details
- Get tribe (ally) data
- Fetch barbarian villages with optional continent filtering
- Support for different Tribal Wars markets (e.g., UK, international)
- Case-insensitive continent matching
- Accurate continent calculation based on village coordinates

### API Endpoints

- `GET /api/tribalwars/villages`: Get all villages
- `GET /api/tribalwars/barbarian-villages`: Get barbarian villages
- `GET /api/tribalwars/players`: Get all players
- `GET /api/tribalwars/tribes`: Get all tribes

For full details on query parameters, please refer to the API documentation.

### Example Usage

Fetch barbarian villages in continent K55 for the UK market, world 1:

```
GET http://localhost:5000/api/tribalwars/barbarian-villages?market=tribalwars.co.uk&worldName=uk1&continent=K55
```

## TribalSentry Discord Bot

### Features

- Monitor barbarian villages across multiple Tribal Wars worlds
- Receive real-time notifications for new barbarian villages
- Manage monitors using Discord slash commands
- View active monitors and their settings
- Enhanced verification process for barbarian villages
- Rich embed messages for notifications and monitor information
- JSON file generation for current barbarian village data

### Bot Commands

- `/addmonitor`: Add a new monitor for barbarian villages
- `/updatemonitor`: Update an existing monitor
- `/removemonitor`: Remove an existing monitor
- `/viewmonitors`: View all active monitors with current barbarian village data

For detailed usage of each command, use the Discord slash command interface.

## Installation

### Prerequisites

- .NET 6.0 SDK or later
- A Discord bot token (obtain from the Discord Developer Portal)

### Steps

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

4. Set up your Discord bot token in the `appsettings.json` file:
   ```json
   {
     "Discord": {
       "BotToken": "YOUR_BOT_TOKEN_HERE"
     }
   }
   ```

## Usage

### Running the API

```
dotnet run --project TribalSentry.API
```

The API will be available at `http://localhost:5000`.

### Running the Discord Bot

```
dotnet run --project TribalSentry.Bot
```

Ensure the API is running before starting the bot.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
