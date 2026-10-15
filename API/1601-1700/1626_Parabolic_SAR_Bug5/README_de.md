# Parabolic SAR Bug5-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Die Parabolic SAR Bug5-Strategie handelt Preisumkehrungen, die vom Parabolic SAR-Indikator erkannt werden. Sie eröffnet eine Long-Position, wenn der Preis über den SAR kreuzt, und eine Short-Position, wenn der Preis darunter kreuzt. Die Strategie kann optional die Handelsrichtung umkehren, offene Positionen bei SAR-Wechseln schließen, und unterstützt Trailing-Stop, Take-Profit und Stop-Loss-Regeln.

## Einstiegsregeln

- **Kaufen** wenn der Preis über den SAR kreuzt und keine Long-Position offen ist.
- **Verkaufen** wenn der Preis unter den SAR kreuzt und keine Short-Position offen ist.
- Wenn `Reverse` aktiviert ist, werden die Signale invertiert.

## Ausstiegsregeln

- Position schließen, wenn das entgegengesetzte SAR-Signal erscheint, wenn `SarClose` aktiviert ist.
- Feste Stop-Loss- und Take-Profit-Ziele anwenden.
- Wenn `Trailing` aktiviert ist, folgt der Stop-Loss dem höchsten (für Longs) oder niedrigsten (für Shorts) Preis seit dem Einstieg.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `Step` | Anfänglicher Beschleunigungsfaktor für Parabolic SAR. |
| `Maximum` | Maximaler Beschleunigungsfaktor für Parabolic SAR. |
| `StopLossPoints` | Stop-Loss-Abstand in Punkten. |
| `TakeProfitPoints` | Take-Profit-Abstand in Punkten. |
| `Trailing` | Trailing-Stop-Verwaltung aktivieren. |
| `TrailPoints` | Trailing-Stop-Abstand in Punkten. |
| `Reverse` | Handelsrichtung umkehren. |
| `SarClose` | Position bei SAR-Wechsel schließen. |
| `CandleType` | Zeitrahmen der zu verarbeitenden Kerzen. |
