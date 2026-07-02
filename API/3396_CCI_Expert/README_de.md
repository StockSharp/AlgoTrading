# CCI Expertenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

Diese Strategie ist eine StockSharp-Konvertierung des ursprünglichen MetaTrader-Roboters „CCI-Expert“. Sie verwendet den Commodity Channel Index (CCI)-Indikator für einen einzelnen Zeitrahmen und hält die Logik streng sequentiell ein: Die Strategie wartet auf drei abgeschlossene Kerzen, bevor sie entscheidet, eine Position zu öffnen oder zu schließen.

## Handelslogik

1. Abonnieren Sie die konfigurierte Kerzenserie und berechnen Sie einen CCI mit dem gewählten Zeitraum.
2. Werten Sie die letzten drei fertigen CCI-Werte aus:
   - **Lange Einrichtung**: Der aktuelle und der vorherige CCI-Wert liegen über `+1`, während der vorletzte Wert unter `+1` lag.
   - **Kurze Einrichtung**: Der aktuelle und der vorherige CCI-Wert liegen unter `+1`, während der vorletzte Wert über `+1` lag.
3. Eröffnen Sie jeweils nur eine Marktposition, wenn keine Position aktiv ist und der Spread-Filter den Handel zulässt.
4. Schließen Sie eine bestehende Position nur, wenn das entgegengesetzte Signal erscheint **und** der Handel bereits profitabel ist (Schlusspreis ist besser als der Einstiegspreis).

## Risikomanagement

- Die Strategie kann entweder einen festen Lot verwenden oder das Volumen aus dem Risikoprozentsatz und der konfigurierten Stop-Loss-Distanz berechnen.
- `StartProtection` platziert automatisch Stop-Loss- und Take-Profit-Klammern in Preispunkten.
- Ein optionaler Spread-Filter blockiert den Handel, bis die aktuelle Geld-/Briefdifferenz unter dem Schwellenwert `MaxSpreadPoints` liegt.

## Parameter

| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `FixedVolume` | Feste Bestellgröße. Auf Null setzen, um die risikobasierte Größenbestimmung zu aktivieren. | 0,1 |
| `RiskPercent` | Prozentsatz des aktuellen Portfoliowerts, der zur Größenbestimmung von Aufträgen verwendet wird, wenn `FixedVolume` Null ist. | 0 |
| `TakeProfitPoints` | Take-Profit-Distanz gemessen in Preispunkten. | 150 |
| `StopLossPoints` | Stop-Loss-Distanz, gemessen in Preispunkten. | 600 |
| `MaxSpreadPoints` | Maximal zulässiger Spread (in Preispunkten). Null deaktiviert den Filter. | 30 |
| `CciPeriod` | Lookback-Zeitraum des Indikators CCI. | 14 |
| `CandleType` | Zeitrahmen der von der Strategie verarbeiteten Kerzen. | 15-Minuten-Kerzen |

## Notizen

- Der CCI-Schwellenwert bleibt genau wie die MQL-Quelle konstant bei `+1` und `-1`, sodass Trades erst nach einem klaren dreistufigen Muster ausgelöst werden.
- Da die risikobasierte Volumengröße auf Instrumentenmetadaten (`PriceStep`, `StepPrice`, `VolumeStep` usw. beruht, stellen Sie sicher, dass diese Werte auf dem angeschlossenen Board verfügbar sind.
- Die Strategie zeichnet Kerzen, die Indikatorlinie CCI und ausgeführte Trades auf dem Diagramm, um das visuelle Debuggen zu erleichtern.
