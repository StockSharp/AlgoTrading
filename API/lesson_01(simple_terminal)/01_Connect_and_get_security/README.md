# StockSharp Trading Connection

## Overview

This repository contains a .NET application using the StockSharp trading framework to connect, configure, and interact with trading securities. It provides UI logic through the `MainWindow.xaml` interface.

## Features

- **Connection Management**: Establish and manage connections with trading services.
- **Settings Configuration**: Load and save connection settings from a JSON configuration file.
- **Security Interaction**: Select and subscribe to securities to receive updates.

## Getting Started

### Prerequisites

Ensure you have the following installed:
- .NET 6
- StockSharp library (available via NuGet)

### Installation

1. Clone the repository.
2. Open the solution in Visual Studio.
3. Restore NuGet packages to resolve dependencies.
4. Build and run the application.

### Usage

1. Launch the application.
2. Use the "Settings" button to configure the connection settings.
3. Connect to the trading service using the "Connect" button.
4. Select a security from the dropdown to start receiving updates.

## Configuration

The application uses a JSON file (`ConnectorFile.json`) for loading and saving configuration settings related to the trading connection.

```csharp
private const string _connectorFile = "ConnectorFile.json";
```

This variable holds the path to the configuration file.

## Code Explanation

### Constructor

The constructor initializes the component and loads settings if the configuration file exists.

```csharp
public MainWindow()
{
    InitializeComponent();

    if (File.Exists(_connectorFile))
    {
        _connector.Load(_connectorFile.Deserialize<SettingsStorage>());
    }
}
```

### Setting_Click Event Handler

This method is triggered when the "Settings" button is clicked. It opens a configuration dialog and saves the settings if the user confirms them.

```csharp
private void Setting_Click(object sender, RoutedEventArgs e)
{
    if (_connector.Configure(this))
    {
        _connector.Save().Serialize(_connectorFile);
    }
}
```

### Connect_Click Event Handler

This method is responsible for starting the connection process. It assigns providers to the SecurityPicker and initiates the connection.

```csharp
private void Connect_Click(object sender, RoutedEventArgs e)
{
    SecurityPicker.SecurityProvider = _connector;
    SecurityPicker.MarketDataProvider = _connector;
    _connector.Connected += Connector_Connected;
    _connector.Connect();
}
```

### Connector_Connected Event Handler

Triggered when the connection is successfully established. It performs an initial lookup of securities.

```csharp
private void Connector_Connected()
{
    // try lookup all securities
    _connector.LookupSecurities(StockSharp.Messages.Extensions.LookupAllCriteriaMessage);
}
```

### SecurityPicker_SecuritySelected Event Handler

Handles the event when a security is selected from the security picker. It subscribes to Level 1 data for the selected security.

```csharp
private void SecurityPicker_SecuritySelected(Security security)
{
    if (security == null) return;
    _connector.SubscribeLevel1(security);
}
```