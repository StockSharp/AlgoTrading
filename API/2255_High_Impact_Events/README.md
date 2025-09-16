# High Impact Events Strategy
[Русский](README_ru.md) | [中文](README_cn.md)

This strategy retrieves the daily economic calendar from ForexFactory and warns the trader before high impact news releases. It is designed to prevent opening new positions shortly before major events that may increase volatility.

When the strategy starts it downloads today's calendar page. Only entries marked as high impact are stored. A timer checks the event list every minute and compares the current time to each event. If an event will occur within the configured window, an informational message is printed.

The strategy does not place any trades. It serves as a risk management helper that can be combined with other strategies to pause trading or adjust positions ahead of impactful news.

## Parameters

- `AlertBeforeMinutes` – minutes before an event to trigger the notification. Default is `5`.

## Workflow

1. Download calendar for the current day from ForexFactory.
2. Parse rows that contain the `calendar__impact--high` marker.
3. Store time, currency, and title for each event.
4. Check the list every minute.
5. Log a message such as `GDP (USD) in 5 minutes.` when the alert window is reached.

Network access must be allowed for the download to succeed. If the calendar cannot be loaded the strategy writes the error message to the log.
