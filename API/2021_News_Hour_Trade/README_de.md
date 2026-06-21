# Nachrichten-Handelsstunden-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **News Hour Trade**-Strategie platziert ausstehende Kauf- und Verkaufs-Stop-Orders rund um geplante Nachrichten-Ereignisse mit hoher Wirkung. Orders werden um eine feste Anzahl von Schritten vom aktuellen Preis versetzt und beinhalten Stop-Loss-, Take-Profit- und optionale Trailing-Stop-Verwaltung.

## Idee

1. Zur konfigurierten Startzeit bereitet sich die Strategie auf eine bevorstehende Nachrichtenveröffentlichung vor.
2. Eine Buy-Stop- und eine Sell-Stop-Order werden `PriceGap` Schritte über und unter dem aktuellen Preis platziert.
3. Wenn eine Order ausgelöst wird, wird die entgegengesetzte ausstehende Order automatisch storniert.
4. Die offene Position ist mit festen Stop-Loss- und Take-Profit-Niveaus geschützt. Wenn `TrailStop` aktiviert ist, folgt das Stop-Niveau dem Preis, wenn er sich zugunsten der Position bewegt.
5. Pro Tag ist nur ein Trade erlaubt.

## Parameter

- **StartHour / StartMinute** – Zeit zum Start des Handels.
- **DelaySeconds** – Pause vor der Order-Platzierung (aktuell informativ).
- **Volume** – Ordergröße in Lots.
- **StopLoss** – Abstand zum Stop-Loss in Preisschritten.
- **TakeProfit** – Abstand zum Take-Profit in Schritten.
- **PriceGap** – Versatz vom aktuellen Preis für ausstehende Orders.
- **Expiration** – Lebensdauer ausstehender Orders in Sekunden (0 bedeutet kein Ablauf).
- **TrailStop** – Trailing-Stop aktivieren.
- **TrailingStop** – Abstand vom aktuellen Preis für Trailing-Stop.
- **TrailingGap** – Mindestlücke vor der Aktualisierung des Trailing-Stops.
- **BuyTrade / SellTrade** – Kauf- oder Verkaufsorders aktivieren.
- **CandleType** – Zeitrahmen für das Zeittracking.

## Hinweise

Die Strategie ist für den M5-Zeitrahmen vorgesehen, kann aber auf jedes Instrument mit niedrigen Spreads angewendet werden. Verwenden Sie sie bei wichtigen Nachrichtenereignissen mit Vorsicht.
