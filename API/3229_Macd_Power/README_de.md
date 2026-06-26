# MACD Power-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die MACD Power-Strategie ist ein Multi-Timeframe-Momentum-System, das aus dem ursprünglichen MetaTrader-Expert-Advisor konvertiert wurde. Die Logik kombiniert ein Paar linear gewichteter gleitender Durchschnitte (LWMA), berechnet auf dem primären Zeitrahmen, zwei MACD-Varianten, einen Momentum-Filter auf einem höheren Zeitrahmen und einen monatlichen MACD-Bias. Die Strategie versucht, an impulsiven Bewegungen teilzunehmen, sobald Momentum und Trendbedingungen des höheren Zeitrahmens übereinstimmen.

## Kernlogik
- **Primäre gleitende Durchschnitte** – Ein schneller und ein langsamer LWMA des typischen Kerzenpreises (\((High + Low + Close) / 3\)). Die Strategie erfordert, dass der schnelle Durchschnitt unter dem langsamen liegt, bevor ein Signal berücksichtigt wird, was den Originalcode widerspiegelt, der auf Rücksetzer innerhalb eines dominanten bearischen Gefälles wartet, bevor in Richtung des monatlichen Bias eingegangen wird.
- **Doppelte MACD-Bestätigung** – Zwei MACD-Indikatoren mit Parametern `(12, 26, 1)` und `(6, 13, 1)` müssen beide über null für Long-Trades oder unter null für Short-Trades liegen. Diese Werte reproduzieren die `MacdMAIN1`- und `MacdMAIN2`-Bedingungen des MQL-Experten, die kurzfristige Beschleunigung messen.
- **Momentum-Filter** – Momentum (Länge 14) wird auf einem höheren Zeitrahmen berechnet, der aus der primären Kerzengröße abgeleitet wird (z.B. 15-Minuten-Basis → 1-Stunden-Momentum). Der absolute Abstand von 100 wird über die letzten drei Momentum-Lesungen überwacht; mindestens eine davon muss den konfigurierten Schwellenwert überschreiten, um zu bestätigen, dass sich der Preis entschlossen bewegt.
- **Monatlicher MACD-Bias** – Ein monatlicher `(12, 26, 9)` MACD (identisch mit `MacdMAIN0`/`MacdSIGNAL0` im EA) muss seine Hauptlinie über der Signallinie für Long-Trades und unter der Signallinie für Shorts haben. Dies schützt vor dem Handel gegen den dominanten Makrotrend.

## Handelsverwaltung
- **Entry-Sizing** – Der Parameter `OrderVolume` definiert die Basis-Ordergröße. Wenn eine Positionsumkehr erforderlich ist, fügt die Engine automatisch die Größe der entgegengesetzten Position hinzu, sodass das Nettovolumen in einer einzigen Market-Order umgekehrt wird.
- **Take-Profit / Stop-Loss** – Absolute Distanzen werden in Instrumentenpunkten ausgedrückt und mit `Security.PriceStep` in Preise umgerechnet (mit sicherem Fallback auf `1`).
- **Trailing-Stop** – Sobald sich der Preis um `TrailingActivationPoints` zugunsten bewegt, verfolgt der Stop den höchsten (Long) oder niedrigsten (Short) Preis mit einem durch `TrailingOffsetPoints` definierten Offset.
- **Break-Even** – Wenn der Preis `BreakEvenTriggerPoints` erreicht, wird ein synthetischer Break-Even-Stop bei `Entry ± BreakEvenOffsetPoints` gesetzt. Wenn der Preis auf dieses Niveau zurückgeht, wird die Position geschlossen.
- **Handelslimit** – `MaxTrades` begrenzt die Anzahl der Positionseröffnungen pro Lauf; sobald der Schwellenwert erreicht ist, werden keine neuen Einstiege ausgegeben.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CandleType` | Primärer Zeitrahmen für die Signalgenerierung. | 15-Minuten-Kerzen |
| `FastMaLength` | Länge des schnellen LWMA (typischer Preis). | 6 |
| `SlowMaLength` | Länge des langsamen LWMA (typischer Preis). | 85 |
| `MomentumLength` | Momentum-Lookback auf dem höheren Zeitrahmen. | 14 |
| `MomentumBuyThreshold` | Minimaler absoluter Abstand von 100 für bullisches Momentum. | 0.3 |
| `MomentumSellThreshold` | Minimaler absoluter Abstand von 100 für bearisches Momentum. | 0.3 |
| `TakeProfitPoints` | Take-Profit-Distanz in Instrumentenpunkten. | 50 |
| `StopLossPoints` | Stop-Loss-Distanz in Instrumentenpunkten. | 20 |
| `TrailingActivationPoints` | Gewinn (Punkte) erforderlich, bevor Trailing aktiviert. | 40 |
| `TrailingOffsetPoints` | Abstand (Punkte) zwischen Trailing-Stop und Extrempreis. | 40 |
| `BreakEvenTriggerPoints` | Gewinn (Punkte), der den Break-Even-Schutz aktiviert. | 30 |
| `BreakEvenOffsetPoints` | Offset (Punkte) beim Verschieben des Stops auf Break-Even. | 30 |
| `MaxTrades` | Maximale Anzahl der Trades pro Sitzung. | 10 |
| `OrderVolume` | Basis-Ordervolumen. | 1 |

## Unterschiede zum MQL-Experten
- Die Strategie verwendet die StockSharp High-Level-API (`SubscribeCandles` + `Bind/BindEx`) anstelle von direktem Tick-Polling. Indikatorwerte werden nur nach abgeschlossenen Kerzen verarbeitet.
- Geldbasierte Trailing- und Eigenkapital-Stop-Blöcke aus dem Originalcode werden nicht portiert, da das kontoebene Geldmanagement normalerweise durch das StockSharp-Risikoframework gehandhabt wird. Stattdessen bleiben punktbasiertes Trailing und Break-Even erhalten und können konfiguriert werden, um das EA-Verhalten zu emulieren.
- Alerts, Benachrichtigungen und manuelle Ordermodifikationshilfen aus MQL werden weggelassen; die StockSharp-Engine verwaltet Orders direkt über Market-Calls.

## Verwendungshinweise
1. Wählen Sie den primären Zeitrahmen durch Einstellen von `CandleType`. Höherer Zeitrahmen-Momentum und monatlicher MACD werden automatisch gemäß dem in `GetMomentumCandleType()` implementierten Mapping abgeleitet.
2. Richten Sie `TakeProfitPoints`, `StopLossPoints` und die Trailing/Break-Even-Parameter an der Tick-Größe des Instruments aus. Die Standardwerte spiegeln die 5-stelligen Forex-Einstellungen des EA wider, können aber für andere Märkte angepasst werden.
3. Überwachen Sie den `MaxTrades`-Zähler bei automatisierten Backtests; setzen Sie ihn auf eine große Zahl, wenn das martingaleartige Stapelverhalten des ursprünglichen EA gewünscht wird.
4. Für die visuelle Analyse aktivieren Sie die Diagrammdarstellung in der GUI – die Implementierung zeichnet standardmäßig Kerzen und die beiden LWMA-Kurven.
