# TribalSentry

TribalSentry is a Discord bot designed to help Tribal Wars players monitor and track barbarian villages across multiple game worlds. It provides real-time notifications for new barbarian villages and those that have been conquered, giving players a strategic advantage in the game.

## Features

- Monitor barbarian villages in multiple Tribal Wars worlds simultaneously
- Receive instant notifications when new barbarian villages appear or are conquered
- Filter monitoring by continent and village points
- Easy-to-use slash commands for managing monitors
- Support for multiple Tribal Wars markets (e.g., UK, international)

## Commands

- `/addmonitor`: Set up a new barbarian village monitor
- `/updatemonitor`: Modify an existing monitor's settings
- `/removemonitor`: Stop monitoring a specific world or continent
- `/viewmonitors`: Display all active monitors and their current data

## Installation

1. Invite the bot to your Discord server using [this link](https://discord.com/oauth2/authorize?client_id=1258909178114740276)
2. Use `/addmonitor` to start monitoring barbarian villages in your desired world

## Usage

Here's a quick example of how to set up a monitor:

```
/addmonitor market:tribalwars.co.uk worldname:uk75 minpoints:300 continent:K55
```

This will monitor barbarian villages in the UK75 world, continent K55, with at least 300 points.

## Self-Hosting

If you want to host the bot yourself:

1. Clone the repository: `git clone https://github.com/Johay90/TribalSentry.git`
2. Set up your Discord bot token in `appsettings.json`
3. Run the API: `dotnet run --project TribalSentry.API`
4. Run the bot: `dotnet run --project TribalSentry.Bot`

## API Configuration

When self-hosting, you need to configure the bot to connect to your API. In the `TribalWarsApiService.cs` file, update the `BaseAddress` to match your API's address.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.
