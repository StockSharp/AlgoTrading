# Geplante Zeithandel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie gibt Marktorders zu einer vordefinierten Zeit ab und schützt sie mit festen Stop-Loss- und Take-Profit-Niveaus.

## Handelsregeln

- Wenn die aktuelle Zeit `Trade Hour:Trade Minute:Trade Second` erreicht, wird die Strategie einmal pro Sitzung ausgelöst.
- Wenn `Allow Buy` aktiviert ist, wird eine Long-Position mit dem angegebenen `Volume` eröffnet.
- Wenn `Allow Sell` aktiviert ist, wird eine Short-Position mit demselben `Volume` eröffnet.
- Schutzorders werden über `StartProtection` mit Punktwerten für Stop-Loss und Take-Profit verwaltet.

## Parameter

| Name | Beschreibung |
| ---- | ------------ |
| `Volume` | Ordergröße. |
| `Take Profit (ticks)` | Take-Profit-Abstand vom Einstieg in Ticks. |
| `Stop Loss (ticks)` | Stop-Loss-Abstand vom Einstieg in Ticks. |
| `Allow Buy` | Long-Trades aktivieren. |
| `Allow Sell` | Short-Trades aktivieren. |
| `Trade Hour` | Handelsstunde des Tages (0-23). |
| `Trade Minute` | Handelsminute der Stunde (0-59). |
| `Trade Second` | Handelssekunde der Minute (0-59). |
| `Candle Type` | Kerzenserie zur Zeitverfolgung, Standard sind 1-Sekunden-Kerzen. |

## Hinweise

Die Strategie eröffnet Trades nur einmal pro Ausführung. Um erneut zu handeln, starten Sie die Strategie neu oder passen Sie die Handelszeit an.
