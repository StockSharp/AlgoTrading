# Andrews Pitchfork-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port des MetaTrader-Expertenberaters "Andrew's Pitchfork". Das Originalskript erwartete ein manuell gezeichnetes Andrews Pitchfork-Objekt und kombinierte es mit Momentum-, Multi-Timeframe-Moving-Average- und MACD-Filtern. Die StockSharp-Version behält den Indikator-Stack, ersetzt das manuelle Zeichnen durch automatische Trenderkennung und recreiert die Schutzlogik (Mehrfach-Einstiegslimits, Stop-Loss, Take-Profit, Break-even und Trailing-Management).

## Strategielogik

1. **Indikatoren**
   - Zwei *Linear Gewichtete Gleitende Durchschnitte* (LWMA), berechnet auf dem typischen Preis der ausgewählten Kerzenserie.
   - Ein *Momentum*-Oszillator auf demselben Zeitrahmen, bewertet durch die absolute Abweichung vom Gleichgewichtsniveau 100.
   - Ein klassisches *MACD (12, 26, 9)*-Signallinienpaar.
2. **Einstiegsregeln**
   - **Long**-Trades erfordern, dass die schnelle LWMA über der langsamen LWMA liegt, mindestens eine der letzten drei Momentum-Abweichungen den `MomentumBuyThreshold` überschreitet und die MACD-Linie über ihrer Signallinie liegt.
   - **Short**-Trades kehren diese Bedingungen um.
   - Die Strategie pyramidiert durch wiederholtes Hinzufügen des Basisvolumens `Volume`, solange die absolute Position unter `Volume * MaxPyramids` liegt. Entgegengesetzte Signale schließen die aktuelle Exposition, bevor die neue Richtung geöffnet wird.
3. **Risikomanagement**
   - Anfängliche Stop-Loss- und Take-Profit-Niveaus werden in Preisschritten um den Einstieg platziert. Beide werden aktualisiert, wenn sich die Positionsgröße ändert.
   - Break-even-Logik verschiebt den Stop, nachdem der Preis eine konfigurierbare Anzahl von Schritten zugunsten der Position zurückgelegt hat.
   - Trailing-Stop-Logik folgt weiterhin dem rentabelsten Preis mit einem zusätzlichen Padding-Abstand.

Im Vergleich zur MQL-Version schlussfolgert der StockSharp-Port den Trend automatisch anhand der LWMA-Steigung, anstatt die Ausrichtung eines benutzergezeichneten Pitchfork-Objekts zu prüfen. Alle anderen Filter (Momentum, MACD, Multi-Order-Limit) und Geldmanagement-Tools wurden mit der High-Level-API von StockSharp reproduziert.

## Parameter

| Name | Typ | Standard | Beschreibung |
|------|------|---------|-------------|
| `CandleType` | `DataType` | 15-Minuten-Zeitrahmen | Primäre Kerzenserie für alle Indikatoren. |
| `FastMaPeriod` | `int` | 6 | Länge der schnellen LWMA auf dem typischen Preis. |
| `SlowMaPeriod` | `int` | 85 | Länge der langsamen LWMA auf dem typischen Preis. |
| `MomentumPeriod` | `int` | 14 | Momentum-Indikator-Rückblick. |
| `MomentumBuyThreshold` | `decimal` | 0.3 | Minimum \|Momentum - 100\| für Long-Einstiege. |
| `MomentumSellThreshold` | `decimal` | 0.3 | Minimum \|Momentum - 100\| für Short-Einstiege. |
| `MaxPyramids` | `int` | 1 | Maximale Anzahl von Basislots in dieselbe Richtung. |
| `StopLossSteps` | `int` | 20 | Stop-Loss-Abstand in Preisschritten. |
| `TakeProfitSteps` | `int` | 50 | Take-Profit-Abstand in Preisschritten. |
| `EnableTrailing` | `bool` | `true` | Aktiviert den dynamischen Trailing Stop. |
| `TrailingTriggerSteps` | `int` | 40 | Gewinn in Schritten vor Aktivierung des Trailing Stops. |
| `TrailingDistanceSteps` | `int` | 40 | Abstand in Schritten zwischen Preisextrem und Trailing Stop. |
| `TrailingPadSteps` | `int` | 10 | Zusätzliches Padding für den Trailing Stop. |
| `EnableBreakEven` | `bool` | `true` | Aktiviert die Break-even-Stop-Anpassung. |
| `BreakEvenTriggerSteps` | `int` | 30 | Gewinn in Schritten vor dem Verschieben des Stops auf Break-even. |
| `BreakEvenOffsetSteps` | `int` | 30 | Offset in Schritten über dem Einstieg bei Break-even-Anwendung. |

## Hinweise

- Die Strategie benötigt einen gültigen `PriceStep` des ausgewählten Wertpapiers, um schrittbasierte Abstände in Preise umzuwandeln. Fehlt der Schritt, bleibt die Trailing- und Break-even-Logik inaktiv.
- Schutzorders (Stop und Take-Profit) werden neu erstellt, wenn sich die Positionsgröße ändert, um sicherzustellen, dass Skalierung oder Umkehrung die Orders an der neuen Exposition ausrichtet.
- Die Standardparameter entsprechen der ursprünglichen EA-Konfiguration, können aber über die integrierten `StrategyParam`-Bereiche optimiert werden.
