# Stochastic Momentum Filter-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Stochastic Momentum Filter-Strategie** ist ein StockSharp-Port des MetaTrader Expert Advisors `Stochastic.mq4` (Ordner `MQL/23473`). Der originale Roboter kombiniert zwei stochastische Oszillatoren, linear gewichtete Moving Averages (LWMA), einen Momentum-Abweichungsfilter und einen höheren Zeitrahmen-MACD-Trendcheck. Diese C#-Version re­kreiert dieselben Bausteine auf der StockSharp High-Level-API und behält den mehrschichtigen Bestätigungsworkflow:

1. **Trendfilter** – ein schneller LWMA muss über (oder unter) einem langsamen LWMA liegen, bevor Long-Trades (oder Short-Trades) erlaubt werden.
2. **Oszillatorbestätigung** – sowohl ein schneller Stochastik (5/2/2) als auch ein langsamer Stochastik (21/4/10) müssen in überverkauften/überkauften Zonen übereinstimmen.
3. **Momentum-Abweichung** – mindestens eine der drei neuesten Momentum-Ablesungen muss um mehr als einen konfigurierbaren Schwellenwert von der 100-Basislinie abweichen und entspricht der Verwendung der MT4-`iMomentum`-Funktion durch den Experten.
4. **Höherer Zeitrahmen-MACD** – die MACD-Hauptlinie auf einem konfigurierbaren höheren Zeitrahmen muss für Longs über der Signallinie bleiben (und für Shorts darunter). Der standardmäßige 30-Tage-Zeitrahmen approximiert den ursprünglichen monatlichen Filter.
5. **RisikoLogik** – Stop-Loss, Take-Profit und optionaler Trailing werden durch `StartProtection` gehandhabt und spiegeln die Schutzparameter des EA wider. Positions-Flips schließen entgegengesetzte Exposition automatisch, bevor die neue Nettoposition aufgebaut wird.

Die Strategie abonniert zwei Kerzenstreams: den Handelszeitrahmen und den höheren Zeitrahmen, der den MACD-Filter speist. Alle Berechnungen werden mit StockSharp-Indikatoren durchgeführt und durch die High-Level-`Bind`-Helfer verarbeitet.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `StochasticBuyLevel` | `30` | Überverkauftes Niveau, das beide stochastischen Oszillatoren für Long-Setups unterschreiten müssen. |
| `StochasticSellLevel` | `80` | Überkauftes Niveau, das beide stochastischen Oszillatoren für Short-Setups erreichen müssen. |
| `FastMaPeriod` | `6` | Länge des schnellen LWMA-Trendfilters. |
| `SlowMaPeriod` | `85` | Länge des langsamen LWMA-Trendfilters. |
| `FastStochasticPeriod` | `5` | `%K`-Periode des schnellen stochastischen Oszillators. |
| `FastStochasticSignal` | `2` | `%D`-Glättungsperiode des schnellen Stochastik. |
| `FastStochasticSmoothing` | `2` | Zusätzliche Glättung des schnellen Stochastik (entspricht MT4-„slowing"). |
| `SlowStochasticPeriod` | `21` | `%K`-Periode des langsamen stochastischen Oszillators. |
| `SlowStochasticSignal` | `4` | `%D`-Glättungsperiode des langsamen Stochastik. |
| `SlowStochasticSmoothing` | `10` | Zusätzliche Glättung des langsamen Stochastik. |
| `MomentumPeriod` | `14` | Lookback des Momentum-Oszillators (wie MT4-`iMomentum`). |
| `MomentumThreshold` | `0.3` | Minimale absolute Abweichung von der 100-Basislinie innerhalb der letzten drei Momentum-Werte. |
| `MacdFastPeriod` | `12` | Schnelle EMA-Periode für den höheren Zeitrahmen-MACD. |
| `MacdSlowPeriod` | `26` | Langsame EMA-Periode für den höheren Zeitrahmen-MACD. |
| `MacdSignalPeriod` | `9` | Signal-EMA-Periode für den höheren Zeitrahmen-MACD. |
| `TakeProfitPoints` | `50` | Take-Profit-Abstand (in Preispunkten). Auf `0` setzen zum Deaktivieren. |
| `StopLossPoints` | `20` | Stop-Loss-Abstand (in Preispunkten). Auf `0` setzen zum Deaktivieren. |
| `EnableTrailing` | `true` | Aktiviert StockSharp-Trailing des Schutz-Stops. |
| `TradeVolume` | `1` | Netto-Positionsgröße bei jedem Signal. |
| `MaxNetPositions` | `1` | Begrenzt die gestapelte Netto-Exposition (multipliziert `TradeVolume`). |
| `CandleType` | `15m` Zeitrahmen | Haupt-Handelszeitrahmen. |
| `HigherTimeframe` | `30d` Zeitrahmen | Zeitrahmen für MACD-Bestätigung. |

## Handelslogik
1. **Indikatorvorbereitung** – die Strategie bindet beide LWMAs, beide stochastischen Oszillatoren, den Momentum-Indikator und den MACD an ihre jeweiligen Kerzenstreams.
2. **Momentum-Historie** – die absolute Distanz des Momentum-Oszillators von 100 wird für die letzten drei abgeschlossenen Balken gespeichert. Dies repliziert die `MomLevelB/MomLevelS`-Arrays des EA.
3. **Einstiegsregeln**
   - **Long**: schneller LWMA über langsamem LWMA, `%K`- und `%D`-Werte beider Stochastiken unter `StochasticBuyLevel`, Momentum-Abweichung über `MomentumThreshold`, und MACD-Hauptlinie über der Signallinie.
   - **Short**: schneller LWMA unter langsamem LWMA, `%K`- und `%D`-Werte beider Stochastiken über `StochasticSellLevel`, Momentum-Abweichung über dem Schwellenwert, und MACD-Hauptlinie unter der Signallinie.
4. **Positions-Handling** – Orders werden mit `BuyMarket`/`SellMarket` gesendet. Wenn ein Umkehrsignal erscheint, schließt die Strategie automatisch jede entgegengesetzte Netto-Exposition, bevor die neue Richtung etabliert wird.
5. **Schutz** – `StartProtection` wendet die konfigurierten Take-Profit- und Stop-Loss-Abstände (in Punkten) an. Wenn `EnableTrailing` true ist, verwaltet StockSharp das Stop-Trailing ähnlich wie die Trailing-Routine des EA.

## Unterschiede zur MQL-Version
- **Volumenskalierung**: der EA skaliert Lotgrößen mit `LotExponent` und erlaubt mehrere gleichzeitige Tickets. Der StockSharp-Port konzentriert sich auf Netto-Exposition und zielt auf ein einzelnes `TradeVolume` pro Richtung ab (begrenzt durch `MaxNetPositions`).
- **Margin-Verwaltung**: Margin-Checks, Equity-Stops und Benachrichtigungsfunktionen aus dem Originalskript werden nicht reproduziert, da sie auf MT4-Konto-APIs angewiesen sind.
- **Freeze-Niveaus**: Broker-spezifische Niedrig-Level-Freeze-Niveau-Prüfungen werden weggelassen; StockSharp-Order-Routing verarbeitet Exchange-Einschränkungen.
- **Break-Even-Toggle**: der MT4-„Move to Breakeven"-Helfer wird durch StockSharp's Trailing-Schutz ersetzt.

## Verwendungshinweise
1. Instrument und Connector zuweisen, dann die Strategie starten. Sie abonniert automatisch sowohl den Handelszeitrahmen als auch den höheren Zeitrahmen für den MACD-Filter.
2. Wenn Ihre Datenquelle keinen 30-Tage-Kerzentyp unterstützt, `HigherTimeframe` auf ein unterstütztes Intervall anpassen (z.B. wöchentlich oder täglich).
3. `TradeVolume` auf Ihre Portfolioeinheiten einstellen.
4. `TakeProfitPoints`/`StopLossPoints` auf null setzen, wenn Schutzorders deaktiviert sein sollen.
5. Alle Kommentare im Code sind auf Englisch geschrieben, und die Einrückung verwendet Tabs.

## Dateien
- `CS/StochasticMomentumFilterStrategy.cs` – StockSharp-Implementierung der Strategielogik.
- `README.md` – englische Dokumentation (diese Datei).
- `README_ru.md` – russische Dokumentation.
- `README_zh.md` – chinesische Dokumentation.
