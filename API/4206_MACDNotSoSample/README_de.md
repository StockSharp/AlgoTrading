# MACD Nicht ganz so beispielhafte Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die MACD Not So Sample-Strategie ist eine Umsetzung des MetaTrader Expert Advisor *MACD_Not_So_Sample*. Der ursprüngliche Roboter handelt
ein 4-Stunden-EURUSD-Chart mit MACD-Crossovers, bestätigt durch einen EMA-Trendfilter, kombiniert mit großen Take-Profit-Niveaus und a
Trailing Stop. Die StockSharp-Version behält die gleiche Struktur bei: Das MACD-Histogramm muss negativ sein und sein Signal überschreiten
Linie für einen Long-Einstieg, während ein positives Histogramm, das unterhalb des Signals kreuzt, einen Short-Einstieg erzeugt. Ein Trend EMA muss das bestätigen
Richtung, bevor eine Position eröffnet wird.

Alle Geldverwaltungsfunktionen sind in StockSharp implementiert: Die Strategie legt ein konfigurierbares Take-Profit-Ziel fest und verwaltet a
Trailing Stop, sobald der Preis weit genug wandert, und schließt Geschäfte, wenn MACD ausreichend in die entgegengesetzte Richtung kreuzt
Stärke. Der Port verwendet StockSharp-Indikatoren und Kerzenabonnements auf hoher Ebene, sodass alle Berechnungen im finalisierten H4 erfolgen
Kerzen, die das MetaTrader-Verhalten widerspiegeln.

## Handelslogik
1. Abonnieren Sie den durch `CandleType` definierten Zeitrahmen (standardmäßig 4-Stunden-Kerzen) und verarbeiten Sie nur fertige Kerzen.
2. Füttern Sie einen `MovingAverageConvergenceDivergenceSignal`-Indikator mit den konfigurierten `FastPeriod`, `SlowPeriod` und
`SignalPeriod`. Der Indikator liefert sowohl die MACD-Linie als auch die Signallinie.
3. Berechnen Sie einen EMA-Trendfilter mit der Länge `TrendPeriod`. Seine Steigung bestimmt, ob lange oder kurze Einträge erlaubt sind.
4. Wandeln Sie die Pip-basierten Schwellenwerte (`MacdOpenLevelPips`, `MacdCloseLevelPips`, `TakeProfitPips`, `TrailingStopPips`) in absolute um
Preisabstände anhand der Pip-Größe des Instruments.
5. Wenn keine Position vorhanden ist:
   - Eröffnen Sie eine **Long-Position**, wenn MACD unter Null liegt, der aktuelle Wert über dem Signalwert liegt und der vorherige MACD darunter lag
Beim vorherigen Signal steigt EMA und die Stärke von MACD überschreitet `MacdOpenLevelPips`.
   - Eröffnen Sie eine **Short**-Position, wenn MACD über Null liegt, der aktuelle Wert unter dem Signalwert liegt und der vorherige MACD darüber lag
Beim vorherigen Signal sinkt EMA und die Stärke von MACD überschreitet `MacdOpenLevelPips`.
6. Beim Halten einer Long-Position:
   - Schließen Sie den Handel, wenn MACD positiv wird, das Signal unterschreitet und seine Größe `MacdCloseLevelPips` überschreitet.
   - Steigen Sie vorzeitig aus, wenn der Preis den konfigurierten Take-Profit erreicht oder wenn das Trailing-Stop-Level durchbrochen wird.
7. Beim Halten einer Short-Position:
   - Schließen Sie den Handel, wenn MACD negativ wird, das Signal überschreitet und seine Größe `MacdCloseLevelPips` überschreitet.
   - Steigen Sie vorzeitig aus, wenn der Preis das Take-Profit-Ziel oder den Trailing Stop erreicht.
8. Der Trailing Stop wird erst aktiviert, wenn der Preis den Schwellenwert um `TrailingStopPips` überschreitet, und fixiert dann den Gewinn um
nach nachfolgenden Kerzenextremen.

## Parameter
| Name | Typ | Standard | Beschreibung |
| --- | --- | --- | --- |
| `FastPeriod` | `int` | `47` | Schnelle EMA-Länge, die in der MACD-Berechnung verwendet wird. |
| `SlowPeriod` | `int` | `166` | Langsame EMA-Länge, die in der MACD-Berechnung verwendet wird. |
| `SignalPeriod` | `int` | `11` | EMA Länge der MACD Signalleitung. |
| `TrendPeriod` | `int` | `8` | Länge des Trendfilters EMA. |
| `MacdOpenLevelPips` | `decimal` | `1` | Mindestgröße MACD (in Pips), die zum Öffnen einer Position erforderlich ist. |
| `MacdCloseLevelPips` | `decimal` | `3` | Mindestgröße MACD (in Pips), die zum Schließen einer Position erforderlich ist. |
| `TakeProfitPips` | `decimal` | `550` | Take-Profit-Distanz, gemessen in Pips. |
| `TrailingStopPips` | `decimal` | `19` | Trailing-Stop-Distanz, gemessen in Pips. Der Wert `0` deaktiviert das Nachstellen. |
| `TradeVolume` | `decimal` | `1` | Für Markteintritte verwendetes Nettovolumen. |
| `CandleType` | `DataType` | Zeitrahmen von 4 Stunden | Von der Strategie verarbeitete Kerzenserie. |
| `RequiredSecurityCode` | `string` | `EURUSD` | Sicherheitscode, der mit dem ausgewählten Instrument übereinstimmen muss und die MetaTrader-Prüfung nachahmt. |

## Unterschiede zum ursprünglichen MetaTrader-Experten
- MetaTrader verwaltet einzelne Bestellungen und magische Zahlen. StockSharp arbeitet mit Nettopositionen, daher schließt die Konvertierung die
aktuelle Belichtung und öffnet eine neue, anstatt mit mehreren Tickets jonglieren zu müssen.
- Der ursprüngliche Code verwendete `AccountFreeMargin`, um die Positionsgröße dynamisch zu bestimmen. Der Port StockSharp stellt einen einfachen `TradeVolume` bereit.
Parameter und Dokumente, die Benutzer die Positionsgröße extern konfigurieren sollten.
- Stop-Loss-Anpassungen nutzen die Candle-Extreme von StockSharp, anstatt bestehende Orders zu ändern. Beim ersten Mal kommt es immer noch zu Ausgängen
Kerze, die gegen den Trailing Stop verstößt und ein Verhalten erzeugt, das der MetaTrader-Logik sehr nahe kommt.
- Alle Indikatorberechnungen basieren auf StockSharp Indikatorklassen, die durch `SubscribeCandles` gebunden sind, ohne direkte Aufrufe von
`iMACD`- oder `iMA`-Funktionen.

## Nutzungshinweise
- Weisen Sie das gewünschte Instrument zu, bevor Sie mit der Strategie beginnen. Wenn der Gerätecode nicht mit `RequiredSecurityCode` übereinstimmt
Die Strategie wird sofort gestoppt, um einen versehentlichen Einsatz auf dem falschen Markt zu verhindern.
- `TradeVolume` wird während `OnStarted` nach `Strategy.Volume` kopiert, sodass Hilfsmethoden (`BuyMarket`, `SellMarket`) immer verwenden
konfigurierte Größe.
- Trailing-Stops werden erst aktiv, wenn der Preis über die konfigurierte Distanz hinaus steigt; Bis dahin wird sich die Strategie auf die stützen
MACD Crossover und Take-Profit-Ziel für Exits.
- Durch das Hinzufügen der Strategie zu einem Diagramm werden Kerzen, beide Indikatoren und ausgeführte Trades gezeichnet, sodass die Crossover-Logik validiert werden kann
visuell.

## Indikatoren
- `MovingAverageConvergenceDivergenceSignal` (MACD Leitung und Signalleitung).
- `ExponentialMovingAverage` (Trendfilter).
