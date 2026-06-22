# Polish Layer-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Polish Layer-Strategie** ist eine Konvertierung des MetaTrader Expert Advisors aus `MQL/17484` in die StockSharp High-Level-API. Sie zielt auf kurzfristige Trendfortsetzung bei Forex-Paaren unter Verwendung von 5-Minuten- oder 15-Minuten-Kerzen. Die Trendrichtung wird durch die Beziehung zwischen schnellen und langsamen exponentiellen gleitenden Durchschnitten und dem jüngsten Momentum des Relative Strength Index (RSI) definiert. Die Einstiegsbestätigung erfordert synchronisierte Signale von Stochastik-Oszillator, DeMarker und Williams %R.

## Indikatoren
- **Exponentieller gleitender Durchschnitt (EMA)** – schnelle (`ShortEmaPeriod`) und langsame (`LongEmaPeriod`) Trendfilter.
- **Relative Strength Index (RSI)** – Momentum-Steigungsfilter aus vorherigen Kerzenwerten.
- **Stochastik-Oszillator** – erkennt Überverkauft-/Überkauft-Umkehrungen über %K-Schwellwert-Kreuzungen.
- **DeMarker** – bestätigt Akkumulations-/Distributionsphasen.
- **Williams %R** – validiert Momentum-Umkehrungen bei extremen Niveaus.

## Parameter
| Parameter | Standardwerte | Beschreibung |
|-----------|---------|-------------|
| `ShortEmaPeriod` | 9 | Länge des schnellen EMA-Trendfilters. |
| `LongEmaPeriod` | 45 | Länge des langsamen EMA-Trendfilters. |
| `RsiPeriod` | 14 | RSI-Lookback für den Momentum-Steigungsvergleich. |
| `StochasticKPeriod` | 5 | Lookback der %K-Linie. |
| `StochasticDPeriod` | 3 | Glättungsperiode für %D. |
| `StochasticSlowing` | 3 | Finaler Verlangsamungsfaktor für %K. |
| `WilliamsRPeriod` | 14 | Williams %R-Lookback-Fenster. |
| `DeMarkerPeriod` | 14 | DeMarker-Lookback-Fenster. |
| `TakeProfitPoints` | 17 | Abstand zum Gewinnziel in Preispunkten (verwendet `Security.PriceStep`). |
| `StopLossPoints` | 77 | Abstand zum Schutzstop in Preispunkten. |
| `CandleType` | 5 Minuten | Von der Strategie verarbeiteter Kerzendatentyp. |
| `Volume` | 1 | Handelsgröße für Markteinträge. |

## Handelslogik
1. **Trendfilter** – die vorherige Kerze muss den schnellen EMA über dem langsamen EMA und einen steigenden RSI zeigen (vorheriger RSI > RSI von zwei Bars zuvor) für Long-Szenarien. Die umgekehrte Konfiguration definiert Short-Szenarien.
2. **Oszillator-Bestätigung** – Einträge werden nur in Betracht gezogen, wenn die Strategie flat ist und alle folgenden Bedingungen erfüllt sind:
   - **Stochastik %K** kreuzt für Longs über 19 oder für Shorts unter 81.
   - **DeMarker** kreuzt für Longs über 0.35 oder für Shorts unter 0.63.
   - **Williams %R** kreuzt für Longs über -81 oder für Shorts unter -19.
3. **Orderausführung** – die Strategie sendet Marktorders mit `BuyMarket(Volume)` oder `SellMarket(Volume)` und verlässt sich auf `StartProtection`, um Stop-Loss- und Take-Profit-Abstände automatisch anzuhängen.

## Risikomanagement
- Schutzorders werden über `StartProtection` erstellt, wobei `TakeProfitPoints` und `StopLossPoints` basierend auf dem Instrument-`PriceStep` in absolute Preisabstände umgewandelt werden.
- Der Algorithmus bleibt außerhalb des Marktes, bis bestehende Positionen durch die Schutzorders geschlossen werden, was das Verhalten des ursprünglichen Expert Advisors widerspiegelt.

## Verwendungshinweise
- Funktioniert am besten bei liquiden Forex-Paaren mit 5-Minuten- oder 15-Minuten-Kerzen.
- Sicherstellen, dass die Wertpapiermetadaten einen gültigen `PriceStep` enthalten; andernfalls `TakeProfitPoints` und `StopLossPoints` an die Tick-Größe des Instruments anpassen.
- Forward-Testing vor dem Live-Einsatz in Betracht ziehen, da die Bestätigungssequenz empfindlich auf Indikatorglättung und Broker-Preisschritte reagiert.
