# Color JJRSX Trend Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie reimaginiert den MetaTrader-Expertenberater `Exp_ColorJJRSX` im StockSharp-High-Level-Framework. Das Originalsystem basiert auf dem proprietären ColorJJRSX-Oszillator, der Jurik-Glättungstechniken zur Erkennung von Trendwechseln kombiniert. In diesem Port wird der Oszillator mit einem Standard-Relative-Strength-Index (RSI) approximiert, der durch einen Jurik Moving Average (JMA) weiter geglättet wird. Die Steigung des geglätteten Oszillators wird dann über mehrere historische Bars ausgewertet, um Ein- und Ausstiege auszulösen.

Der Handel findet auf einem konfigurierbaren Kerzen-Zeitrahmen (standardmäßig 4-Stunden-Kerzen) statt und unterstützt unabhängige Umschalter für Long- und Short-Operationen. Zusätzliche Parameter ermöglichen es, die Ausstiegslogik identisch zum Quell-Expertenberater zu halten, während native StockSharp-Risikokontrollen wie punktbasierter Stop-Loss und Take-Profit eingeführt werden.

## Indikatorkonstruktion
1. **RSI-Approximation** – Ein `RelativeStrengthIndex` mit dem durch `JurxPeriod` definierten Zeitraum ersetzt die ursprüngliche JurX-Glättungsstufe. Dies hält den Oszillator zwischen 0 und 100 begrenzt, während relatives Momentum erfasst wird.
2. **Jurik-Glättung** – Die RSI-Ausgabe wird durch einen `JurikMovingAverage` (Länge `JmaPeriod`) geleitet. Die resultierende Reihe ist eine glatte Kurve, die schnell auf Momentum-Änderungen reagiert, ohne übermäßige Verzögerung.
3. **Historisches Fenster** – Die Strategie speichert die letzten `SignalBar + 3` JMA-Werte, um die `CopyBuffer`-Verwendung aus MQL zu replizieren. Werte, die durch `SignalBar`, `SignalBar + 1` und `SignalBar + 2` indexiert werden, entsprechen den Bars, die im Quellexperten für die Signalauswertung verwendet werden.

## Handelslogik
- **Bullisches Setup**
  - `JMA[SignalBar + 1] < JMA[SignalBar + 2]` bestätigt, dass der Oszillator auf der vorherigen Bar nach oben gedreht hat.
  - `JMA[SignalBar] > JMA[SignalBar + 1]` zeigt, dass das Aufwärtsmomentum auf der letzten geschlossenen Bar anhält.
  - Wenn Long-Einstiege aktiviert sind und keine Long-Position aktiv ist, kauft die Strategie `OrderVolume` Einheiten. Bestehendes Short-Exposure wird automatisch umgekehrt.
- **Bärisches Setup**
  - `JMA[SignalBar + 1] > JMA[SignalBar + 2]` bestätigt eine Abwärtsdrehung.
  - `JMA[SignalBar] < JMA[SignalBar + 1]` validiert anhaltenden Abwärtsschwung.
  - Wenn Short-Einstiege aktiviert sind, verkauft die Strategie `OrderVolume` Einheiten und dreht bestehendes Long-Exposure um.
- **Ausstiegsregeln**
  - Wenn die Steigung des geglätteten Oszillators gegen die Position dreht (`AllowBuyClose` / `AllowSellClose`), wird der offene Trade zum Markt geschlossen.
  - Schutz-Stop-Loss- und Take-Profit-Niveaus (in Preispunkten ausgedrückt) werden bei jeder neuen Position neu berechnet. Wenn der Kerzenbereich ein Niveau berührt, wird die Position sofort geschlossen.

## Risikomanagement
- `StopLossPoints` wird mit dem Instrument-Preisschritt in Preisabstand umgerechnet und schützt vor ungünstigen Bewegungen.
- `TakeProfitPoints` definiert den symmetrischen Zielabstand.
- Stops und Ziele werden automatisch deaktiviert, wenn sie auf null gesetzt werden.
- Das Volumen kann unabhängig vom Basis-Strategievolumen durch `OrderVolume` feinabgestimmt werden.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `JurxPeriod` | Zeitraum der RSI-Approximation vor der Jurik-Glättung. Entspricht dem JurX-Zeitraum des MQL-Experten. |
| `JmaPeriod` | Länge des Jurik Moving Average, der auf die RSI-Ausgabe angewendet wird. |
| `SignalBar` | Index der historischen Bar für die Auswertung (1 = vorherige geschlossene Bar). Größere Werte verzögern die Signalbestätigung. |
| `EnableBuy` / `EnableSell` | Long- oder Short-Einstiege unabhängig umschalten. |
| `AllowBuyClose` / `AllowSellClose` | Steigungsbasierte Ausstiegssignale für Long- bzw. Short-Positionen aktivieren. |
| `OrderVolume` | Volumen, das bei jedem neuen Einstieg gehandelt wird. Bestehendes entgegengesetztes Exposure wird zur neuen Order addiert, um eine vollständige Umkehrung durchzuführen. |
| `TakeProfitPoints` / `StopLossPoints` | Gewinnziel und Stop-Abstand in Instrument-Punkten. Auf null setzen, um zu deaktivieren. |
| `CandleType` | Kerzen-Zeitrahmen für Indikatorberechnungen (standardmäßig 4-Stunden-Kerzen). |

## Unterschiede zum originalen Expertenberater
- JurX-Glättung wird durch einen klassischen RSI approximiert, da der proprietäre JurX-Algorithmus in StockSharp nicht verfügbar ist. Parameternamen bleiben konsistent, um die Migration zu vereinfachen.
- MetaTrader-Slippage (`Deviation_`) und Money-Management-Enumerationen werden nicht reproduziert. Stattdessen wird ein fester `OrderVolume`-Parameter bereitgestellt; Sie können ihn mit StockSharp-Positionsgrößen-Modulen kombinieren, wenn erforderlich.
- Orders werden mit `BuyMarket`/`SellMarket` ausgeführt, während Stop-Loss und Take-Profit durch Preisprüfungen auf der fertigen Kerze emuliert werden.

## Verwendungstipps
1. Hängen Sie die Strategie an das gewünschte Instrument an und setzen Sie `CandleType` entsprechend dem Zeitrahmen, den Sie replizieren möchten.
2. Passen Sie `JurxPeriod` und `JmaPeriod` an die Reaktionsfähigkeit des Marktes an. Höhere Werte erzeugen glattere Schwingungen und weniger Signale.
3. Optimieren Sie `SignalBar`, wenn Sie eine zusätzliche Bestätigungsverzögerung im Vergleich zur Standard-Ein-Bar-Verzögerung benötigen.
4. Konfigurieren Sie `OrderVolume`, `StopLossPoints` und `TakeProfitPoints` entsprechend Ihrem Risikoappetit. Null verwenden, um automatische Ausstiege zu deaktivieren.
5. Kombinieren Sie mit StockSharp's eingebauten Protokollierungs- oder Diagramm-Helpers (bereits für Kerzen + Indikatorplots verdrahtet), um das Oszillatorverhalten in Echtzeit zu überwachen.

Die Strategie ist sowohl für diskretionäre Experimente als auch für automatisiertes Backtesting innerhalb der StockSharp-Umgebung bereit, während sie der Absicht des originalen ColorJJRSX-Systems treu bleibt.
