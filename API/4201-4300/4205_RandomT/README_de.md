# RandomT-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine StockSharp-Portierung des MetaTrader 4-Expertenberaters „RandomT“. Das ursprüngliche EA wartet auf einen ZigZag-Schwung, der mit einem bestätigten Fraktal übereinstimmt, und filtert dann den Eintrag mit einem MACD-Vergleich. Die StockSharp-Version behält den gleichen Entscheidungsprozess bei: Sie überwacht eine konfigurierbare Anzahl von Kerzen (`BarWatch`), bestätigt, dass ein Fünf-Balken-Fraktal das jüngste Swing-Extrem markiert, und handelt nur, wenn die MACD-Hauptlinie über oder unter der Signallinie auf demselben historischen Balken liegt.

## Handelslogik
- Erstellen Sie rollierende Kerzenpuffer und berechnen Sie das MACD-Signal für jeden fertigen Balken des ausgewählten Zeitrahmens (`CandleType`).
- Schauen Sie sich `Shift` Balken in der Vergangenheit an und prüfen Sie, ob dieser Balken ein Auf- oder Ab-Fraktal bildet (zwei Kerzen auf jeder Seite).
- Validieren Sie das Fraktal anhand der umgebenden Preisbewegung: Das Hoch muss der größte Wert oder das Tief der kleinste Wert innerhalb des Lookback-Fensters `BarWatch` sein. Dies spiegelt die ZigZag-Schwungbestätigung wider, die von der MetaTrader-Version verwendet wird.
- Bei einem kurzen Setup muss der Hauptwert MACD größer sein als der Signalwert auf dem verschobenen Balken. Für einen langen Aufbau muss der umgekehrte Vergleich zutreffen.
- Wenn ein Signal erscheint, verwendet die Strategie eine einzelne Marktorder, deren Volumen jede Gegenposition neutralisiert, bevor der neue Handel eröffnet wird.

## Trailing-Stop-Management
- Der Trailing-Block wird nur aktiviert, wenn `UseTrailingProfit` aktiviert ist und der variable Gewinn (umgerechnet durch `PriceStep` und `StepPrice`) `MinProfit` übersteigt.
- Die Nachlaufdistanz wird in Preispunkten gemessen. Wenn `AutoStopLevel` den Wert `true` hat, verwendet die Engine `StartStopLevelPoints`; andernfalls wird `StopLevelPoints` verwendet.
- Bei Long-Positionen folgt der Stop `ClosePrice - distance`, bei Short-Positionen folgt er `ClosePrice + distance`. Wenn die Kerze das Stop-Level durchbricht, schließt die Strategie den Handel mit einer Marktorder.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `TradeVolume` | Basishandelsgröße in Lots, die für jeden Eintrag verwendet wird. |
| `BarWatch` | Anzahl der Balken, die verwendet werden, um zu bestätigen, dass ein Fraktal auch ein ZigZag-Schwung-Extrem ist. |
| `Shift` | Anzahl der zurückliegenden Balken im Verlauf, die auf Signale hin ausgewertet werden. Sollte für klassische Fraktale bei 2 bleiben. |
| `UseTrailingProfit` | Aktiviert die Trailing-Stop-Logik. |
| `AutoStopLevel` | Ändert die Nachlaufdistanz auf `StartStopLevelPoints`. |
| `StartStopLevelPoints` | Alternative Nachlaufdistanz (Punkte). |
| `StopLevelPoints` | Primäre Nachlaufdistanz (Punkte). |
| `MinProfit` | Mindestens erforderlicher variabler Gewinn (Kontowährung), bevor das Trailing angewendet wird. |
| `CandleType` | Für Kerzen und Indikatorberechnungen verwendeter Zeitrahmen. |
| `MacdFastLength` | Schneller Zeitraum von EMA für den Filter MACD. |
| `MacdSlowLength` | Langsamer Zeitraum von EMA für den Filter MACD. |
| `MacdSignalLength` | Signalzeitraum von EMA für den Filter MACD. |

## Notizen
- Die Strategie berechnet Fraktale intern (zwei Balken auf jeder Seite) und verwendet das Ergebnis für die ZigZag-Validierung wieder, wobei es eng mit den Puffern übereinstimmt, auf die im MQL-Code zugegriffen wird.
- Die ZigZag-Bestätigung wird angenähert, indem die umgebenden `BarWatch`-Kerzen überprüft werden, anstatt den vollständigen MetaTrader-Indikator erneut auszuführen, wodurch das Verhalten innerhalb von StockSharp deterministisch bleibt.
- Der Trailing-Stop-Gewinn wird aus `PriceStep` und `StepPrice` des Instruments abgeleitet. Überprüfen Sie diese Werte für Ihr Instrument, bevor Sie die Strategie ausführen.
