# MonopolyServer
![phrolova-phrolova-ult](https://github.com/user-attachments/assets/5ea711db-f251-40f6-a496-a9c3f0106c55)

A real-time, multi-player Monopoly game server built with C# ASP.NET Core and SignalR.

## Features

-   **Real-time Gameplay:** Utilizes SignalR for real-time communication between players and the server.
-   **Core Monopoly Mechanics:** Implements essential Monopoly game rules, including property management, trading, and bankruptcy.
-   **Player Management:** Allows players to join and leave games, with support for player-specific actions.
-   **Game State Management:** Manages the game state, including player turns, property ownership, and game phases.
-   **Event Publishing:** Uses an event publisher to notify other services about game events.
-   **Authentication:** Implements JWT-based authentication with access and refresh tokens.
-   **Database Integration:** Uses Entity Framework Core for database interactions.
-   **Swagger API Documentation:** Provides Swagger API documentation for easy exploration and testing.

## Technologies Used

-   C#
-   ASP.NET Core
-   SignalR
-   Entity Framework Core
-   JWT Authentication
-   Swagger
-   Kafka (for event publishing)

## Project Structure

-   `.gitignore`: Specifies intentionally untracked files that Git should ignore.
-   `appsettings.Example.json`: Example configuration file.
-   `docker-compose.yaml`: Docker Compose file for running the application in a container.
-   `Dockerfile`: Docker file for building the application's Docker image.
-   `MonopolyServer.csproj`: C# project file.
-   `MonopolyServer.sln`: C# solution file.
-   `Program.cs`: Entry point of the application.
-   `README.md`: This file, providing an overview of the project.
-   `Migrations/`: Contains Entity Framework Core migrations.
-   `MonopolyServer.Tests/`: Contains unit tests for the project.
-   `Properties/`: Contains launch settings for the application.
-   `src/`: Contains the source code of the application.
    -   `Database/`: Contains database-related files.
        -   `MonopolyDbContext.cs`: Database context for the application.
        -   `Entities/`: Contains entity definitions.
            -   `User.cs`: User entity.
            -   `UserOAuth.cs`: User OAuth entity.
        -   `Enums/`: Contains enums.
            -   `ProviderName.cs`: Provider name enum.
    -   `DTO/`: Contains Data Transfer Objects.
        -   `UserDTO.cs`: User DTO.
        -   `UserOAuthDTO.cs`: User OAuth DTO.
    -   `Enums/`: Contains enums related to the game.
        -   `ChanceOutcome.cs`: Chance outcome enum.
        -   `ColorGroup.cs`: Color group enum.
        -   `GamePhase.cs`: Game phase enum.
        -   `RentStage.cs`: Rent stage enum.
        -   `SpecialSpaceType.cs`: Special space type enum.
        -   `TransactionType.cs`: Transaction type enum.
        -   `TreasureOutcome.cs`: Treasure outcome enum.
    -   `Hubs/`: Contains SignalR hubs.
        -   `GameHubs.cs`: Game hub for real-time communication.
    -   `Models/`: Contains model definitions for game entities.
        -   `Board.cs`: Board model.
        -   `ChanceCard.cs`: Chance card model.
        -   `CountryProperty.cs`: Country property model.
        -   `Game.cs`: Game model.
        -   `GameConfig.cs`: Game configuration model.
        -   `Player.cs`: Player model.
        -   `Property.cs`: Property model.
        -   `RailroadProperty.cs`: Railroad property model.
        -   `Space.cs`: Space model.
        -   `SpecialSpace.cs`: Special space model.
        -   `Trade.cs`: Trade model.
        -   `TransactionHistory.cs`: Transaction history model.
        -   `TransactionInfo.cs`: Transaction info model.
        -   `TreasureCard.cs`: Treasure card model.
        -   `UtilityProperty.cs`: Utility property model.
    -   `Repositories/`: Contains repositories for data access.
        -   `IUserOAuthRepository.cs`: User OAuth repository interface.
        -   `IUserRepository.cs`: User repository interface.
        -   `UserOAuthRepository.cs`: User OAuth repository implementation.
        -   `UserRepository.cs`: User repository implementation.
    -   `Routes/`: Contains route definitions.
        -   `AuthRoute.cs`: Authentication routes.
        -   `GameRoute.cs`: Game routes.
    -   `Services/`: Contains services.
        -   `GameManager.cs`: Manages the active games and handles game logic.
        -   `IEventPublisher.cs`: Event publisher interface.
        -   `KafkaEventPublisher.cs`: Kafka event publisher implementation.
        -   `KafkaSignalRNotifierService.cs`: Kafka SignalR notifier service.
        -   `Auth/`: Contains authentication services.
            -   `AuthService.cs`: Authentication service.
            -   `DiscordAccountResponse.cs`: Discord account response model.
            -   `DiscordTokenResponse.cs`: Discord token response model.
    -   `Utils/`: Contains utility classes.
        -   `Helpers.cs`: Helper functions.
        -   `RollResult.cs`: Roll result model.

## Setup Instructions

1.  **Clone the repository:**

    ```bash
    git clone https://github.com/reyhaonan/MonopolyServer.git
    ```
2.  **Navigate to the project directory:**

    ```bash
    cd MonopolyServer
    ```
3.  **Install the .NET Core SDK:**

    Download and install the .NET Core SDK from [https://dotnet.microsoft.com/download](https://dotnet.microsoft.com/download).
4.  **Restore dependencies:**

    ```bash
    dotnet restore
    ```
5.  **Build the project:**

    ```bash
    dotnet build
    ```
6.  **Run the application:**

    ```bash
    dotnet run
    ```

## Configuration

-   Configure the application settings in `appsettings.json`.
-   Set the `AllowedOrigins` in `appsettings.json` to the allowed origins for CORS.
-   Configure the database connection string in `appsettings.json`.
-   Configure the Kafka settings in `appsettings.json`.

## Contribution Guidelines

Contributions are welcome! Please follow these guidelines:

1.  Fork the repository.
2.  Create a new branch for your feature or bug fix.
3.  Write tests for your changes.
4.  Submit a pull request.

## License

[MIT](https://opensource.org/licenses/MIT)
