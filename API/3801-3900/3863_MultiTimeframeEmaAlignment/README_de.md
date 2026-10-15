# Strategie MultiTimeframeEmaAlignmentStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **MultiTimeframeEmaAlignmentStrategy** ist ein StockSharp-Port des MetaTrader 4 Expert Advisors `1h-4h-1d.mq4` aus dem Ordner `MQL/7713`. Der ursprüngliche Roboter gleicht schnelle und langsame exponentielle gleitende Durchschnitte über drei Zeitrahmen an und wendet ein schützendes Geldmanagement über feste Stop-Loss-, Take-Profit- und Trailing-Stop-Levels an. Diese C#-Version folgt der gleichen Grundidee und nutzt dabei die Indikatorbindungen und Ordnungshelfer von StockSharp.

## Handelslogik
- Die Strategie abonniert gleichzeitig drei Kerzenserien: M1 (Signalzeitrahmen), M5 (Mittelfristfilter) und M30 (Trendbestätigung mit höherem Zeitrahmen).
- Jede Serie speist ein Paar exponentieller gleitender Durchschnitte (EMA) mit konfigurierbaren Längen (Standard 8 und 64).
- Ein **bullisches Setup** erfordert, dass der schnelle EMA in allen drei Zeitrahmen über dem langsamen EMA bleibt. Außerdem darf der schnelle EMA nicht an Schwung verlieren (aktueller Wert größer oder gleich dem vorherigen Wert und auch über dem Wert vor `ShiftDepth` Balken).
- Ein **bärisches Setup** erfordert, dass der schnelle EMA in allen drei Zeitrahmen unter dem langsamen EMA bleibt, wobei der schnelle EMA an Dynamik verliert.
- Aufträge werden beim Schließen der M1-Kerze ausgelöst, wenn die Ausrichtungs- und Momentumprüfungen erfüllt sind. Long-Signale sind nur zulässig, wenn keine Long-Position offen ist (oder ein bestehender Short zuerst geschlossen wird) und umgekehrt.

Diese Interpretation stellt die Absicht der MT4-Bedingungen mit dem übergeordneten API von StockSharp wieder her. Die MQL „MA-Shift“-Vergleiche werden durch den `ShiftDepth`-Puffer emuliert, der EMA-Werte ein paar Kerzen zurückverfolgt und sicherstellt, dass die Dynamik mit der Einstiegsrichtung übereinstimmt.

## Risikomanagement
- Die Positionsgröße wird durch den Parameter `TradeVolume` gesteuert (standardmäßig 3 Lots wie beim Original EA).
- Optionale Stop-Loss- und Take-Profit-Abstände werden in Pips angegeben. Sie werden über den `PriceStep` des Instruments in Preise umgewandelt (fällt bei Fehlen auf `0.0001` zurück).
- Der Trailing-Stop reproduziert das Verhalten von EA, indem er den Stop-Preis immer dann näher an den Markt verschiebt, wenn der Handel weit genug voranschreitet.
- Risikoparameter können unabhängig voneinander umgeschaltet werden und entsprechen den Flags `StopLossMode`, `TakeProfitMode` und `TrailingStopMode` aus dem Skript MQL.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `TradeVolume` | Bestellvolumen, das von `BuyMarket` / `SellMarket` verwendet wird. Spiegelt die Eingabe `Lots`. | `3` |
| `FastLength` | EMA Zeitraum für die Schnellleitung. | `8` |
| `SlowLength` | EMA Zeitraum für die langsame Leitung. | `64` |
| `ShiftDepth` | Anzahl der historischen Kerzen, die zur Emulation der MQL gleitenden Durchschnittsverschiebungsvergleiche verwendet werden. | `3` |
| `UseStopLoss` | Ermöglicht einen festen Stop-Loss. | `true` |
| `StopLossPips` | Stop-Loss-Distanz, ausgedrückt in Pips. | `75` |
| `UseTakeProfit` | Ermöglicht Take-Profit. | `true` |
| `TakeProfitPips` | Take-Profit-Distanz, ausgedrückt in Pips. | `150` |
| `UseTrailingStop` | Aktiviert die Trailing-Stop-Verwaltung. | `true` |
| `TrailingStopPips` | Nachlaufdistanz in Pips. | `30` |
| `M1CandleType` | Kerzentyp für den Signalzeitrahmen (Standard 1 Minute). | `1m` |
| `M5CandleType` | Kerzentyp für den Halbzeitfilter (Standard 5 Minuten). | `5m` |
| `M30CandleType` | Kerzentyp für den höheren Zeitrahmen (Standard 30 Minuten). | `30m` |

## Nutzungshinweise
1. Hängen Sie die Strategie an ein Instrument an und stellen Sie sicher, dass historische Daten für alle drei Zeitrahmen verfügbar sind, damit die EMA-Puffer gefüllt werden können.
2. Der Parameter `ShiftDepth` sollte mindestens `2` betragen, damit die Strategie die kurzfristige Dynamik validieren kann.
3. Wenn `UseTrailingStop` ohne `UseStopLoss` aktiv ist, initialisiert die abschließende Logik immer noch einen Stoppwert, sobald sich der Handel positiv entwickelt.
4. Da StockSharp bei Kerzenschluss ausgeführt wird, können die Ergebnisse leicht von der Tick-für-Tick-Ausführung der MT4-Version abweichen, insbesondere auf volatilen Märkten. Das zentrale Trendausrichtungsverhalten bleibt erhalten.

## Konvertierungshinweise
- Indikatorberechnungen basieren ausschließlich auf dem `Bind`-Mechanismus von StockSharp; Es werden keine manuellen Indikatorverlaufssammlungen verwendet.
- Die Auftragsverwaltung wird mit High-Level-Helfern (`BuyMarket`, `SellMarket`) und interner Preisverfolgung anstelle direkter `OrderSend`-Aufrufe implementiert.
- E-Mail-Benachrichtigungen und Slippage-Kontrollen aus dem MQL-Skript werden weggelassen, da sie außerhalb des Geltungsbereichs von StockSharp liegen.

## Dateien
- `CS/MultiTimeframeEmaAlignmentStrategy.cs` – Hauptimplementierung der C#-Strategie.
- `README_ru.md` – Russische Dokumentation.
- `README_zh.md` – Chinesische Dokumentation.
