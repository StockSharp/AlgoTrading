# Flache Trendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Flat-Trend-Strategie** reproduziert die Kernideen des ursprünglichen Flat-Trend-Expertenberaters durch die Kombination von Multi-Speed-Trendfiltern, ADX-Bestätigung und einem Standardabweichungs-„Saft“-Breakout-Filter. Die Strategie konzentriert sich darauf, den Moment zu erkennen, in dem der Preis eine Schwankungsphase verlässt und das Momentum zunimmt, sodass er Richtungsbewegungen mit dynamischem Positionsschutz verbinden kann.

## Handelslogik
1. **Trendfilter** – drei exponentielle gleitende Durchschnitte (EMAs) mit konfigurierbarer Länge stellen den Auslöser, den ersten Filter und den zweiten Filter dar. Ihre Steigung und die Preisposition relativ zu jedem EMA werden in Zustände übersetzt:
   - Stark bullisch (Preis über EMA und EMA steigend).
   - Moderat bullisch (Preis über EMA, aber Steigung neutral).
   - Stark bärisch (Preis unter EMA und EMA fallend).
   - Mäßig rückläufig (Preis unter EMA, aber Steigung neutral).
2. **Eintrittsregeln**
   - Long-Trades erfordern bullische Zustände am Auslöser und Filter EMA. Der zweite Filter kann optional ignoriert werden. Der strikte Modus erzwingt die Verwendung nur starker bullischer Staaten.
   - Short-Trades spiegeln die Bedingungen für bärische Zustände wider.
   - Die optionale ADX-Bestätigung stellt sicher, dass der durchschnittliche Richtungsindex einen Schwellenwert überschreitet und, wenn aktiviert, die +DI- und –DI-Komponenten mit der Handelsrichtung übereinstimmen.
   - Der „Saft“-Filter überprüft, ob die Standardabweichung der Preise über einem benutzerdefinierten Ausbruchsniveau liegt, und verhindert so Trades in Phasen flacher Volatilität.
   - Der Handel kann auf ein ausgewähltes Intraday-Fenster beschränkt werden.
3. **Ausgangsregeln**
   - Entgegengesetzte Trendzustände am Auslöser EMA initiieren einen Exit. Im strikten Modus wartet die Strategie auf das stärkste Gegensignal.
   - Dynamische Stopps bei Ausstiegspositionen immer dann, wenn der Preis das berechnete Stop-Level berührt.

## Risikomanagement
- **Erster Stopp** – wird entweder aus einer statischen Pip-Distanz oder aus dem Average True Range (ATR)-Wert berechnet und emuliert die ADR-basierte Logik des ursprünglichen EA.
- **Trailing Stop** – bewegt sich mit dem höchsten (oder niedrigsten) Preis seit der Eingabe unter Verwendung von ATR multipliziert mit einem Divisor.
- **Break-Even** – sobald der Preis um die konfigurierte Distanz steigt, bewegt sich der Stop um einen kleinen Sperrwert über den Einstiegspreis hinaus.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `TriggerLength` | EMA Länge für den Triggerfilter. |
| `FilterLength1` | EMA Länge für den ersten Bestätigungsfilter. |
| `FilterLength2` | EMA Länge für den zweiten Bestätigungsfilter. |
| `UseOnlyPrimaryIndicators` | Für Einträge nur Trigger und ersten Filter verwenden. |
| `IgnoreModerateForEntry` | Erfordern starke Trendzustände für neue Trades. |
| `IgnoreModerateForExit` | Erfordern starke Gegensignale, um Geschäfte abzuschließen. |
| `UseTradingHours` | Aktivieren Sie das Intraday-Handelsfenster. |
| `TradingHourBegin` / `TradingHourEnd` | Start- und Endzeit des Handelsfensters. |
| `UseJuiceFilter`, `JuicePeriod`, `JuiceThreshold` | Parameter des Standardabweichungs-Breakout-Filters. |
| `UseAdxFilter`, `AdxPeriod`, `AdxThreshold`, `UseDirectionalFilter` | ADX Stärke und DI-Bestätigung. |
| `UseAdrForStop`, `StopLossPips` | Anfängliche Stop-Loss-Konfiguration. |
| `TrailingDivisor` | ATR-Multiplikator für die Trailing-Stop-Berechnung. |
| `BreakEvenPips`, `BreakEvenLockPips` | Break-Even-Aktivierung und Sperrdistanz. |
| `AtrPeriod` | ATR-Lookback, der zur Volatilitätsschätzung verwendet wird. |
| `CandleType` | Zeitrahmen der primären Kerze. |

## Zusammenfassung der Indikatoren
- **Exponentieller gleitender Durchschnitt (EMA)** – drei Instanzen für die Trendbewertung mit mehreren Geschwindigkeiten.
- **Standardabweichung** – modelliert den „Saft“-Volatilitäts-Breakout-Filter.
- **Average True Range (ATR)** – misst die Volatilität für Stops und Trailing.
- **Durchschnittlicher Richtungsindex (ADX)** – bestätigt die Stärke und Richtung des Trends.

## Nutzungshinweise
1. Stellen Sie sicher, dass die Strategiesicherheit über einen definierten `PriceStep` verfügt. andernfalls wird für Pip-basierte Abstände der Standardschritt von 0,0001 verwendet.
2. Die Strategie verwendet Marktaufträge (`BuyMarket`, `SellMarket`) und skaliert das Volumen automatisch, wenn Positionen umgekehrt werden.
3. Dynamische Stopps werden intern simuliert, indem Positionen geschlossen werden, wenn das virtuelle Stoppniveau berührt wird.
4. Kombinieren Sie das Handelsfenster und strenge Einstiegsoptionen, um sich auf Sitzungen mit hoher Liquidität zu konzentrieren und unruhige Phasen zu vermeiden.
