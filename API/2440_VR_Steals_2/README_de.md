# VR Steals 2-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine StockSharp-Konvertierung des MetaTrader-5-Experten "VR---STEALS-2". Sie eröffnet eine einzelne Long-Position und demonstriert einfaches Positionsmanagement ohne Indikatoren.

## Funktionsweise
1. Beim Start kauft die Strategie über `BuyMarket` und speichert den Einstiegspreis.
2. Candledaten (standardmäßig 1 Minute) werden über `SubscribeCandles` abonniert.
3. Für jede abgeschlossene Kerze:
   - Wenn sich der Preis um `Breakeven` Schritte zugunsten des Trades bewegt hat, wird das Stop-Level um `BreakevenOffset` Schritte über den Einstieg verschoben.
   - Erreicht der Preis den Einstieg plus `TakeProfit` Schritte, wird die Position geschlossen.
   - Fällt der Preis auf das Stop-Level (initial `StopLoss` unterhalb des Einstiegs oder der verschobene Break-Even-Stop), wird die Position geschlossen.
4. Nach dem Ausstieg eröffnet die Strategie keine neuen Positionen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| TakeProfit | Abstand in Preisschritten zum Take-Profit-Level. | 50 |
| StopLoss | Anfänglicher Stop-Abstand in Preisschritten. | 50 |
| Breakeven | Gewinn in Schritten, der zum Aktivieren des Break-Even-Stops benötigt wird. | 20 |
| BreakevenOffset | Versatz über den Einstieg beim Setzen des Break-Even-Stops. | 9 |
| CandleType | Kerzentyp für die Preisverarbeitung. | 1-Minuten-Zeitrahmen |

`StartProtection()` wird verwendet, um den integrierten Positionsschutz zu aktivieren.
