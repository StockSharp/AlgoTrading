# Strategie Profit Hunter HSI with Fibonacci Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine C#-Portierung des MetaTrader 4 Expert Advisors `Profit_Hunter_HSI_with_fibonacci.mq4`. The original script combines
ein Intraday-Filter für den exponentiellen gleitenden Durchschnitt (EMA) mit Fibonacci Retracement-Zonen, abgeleitet vom Tages-Chart. Der StockSharp
Die Implementierung folgt der gleichen Idee unter Verwendung des High-Level-API: Es abonniert zwei Candle-Streams (Intraday und Daily) und berechnet
Das Fibonacci-Raster generiert dynamisch Handelssignale, wenn der Preis mit diesen Bändern interagiert, und verwaltet die resultierende Position
mit adaptiver Stop-Platzierung und einer abgestuften Trailing-Stop-Logik.

## Marktdatenfluss
1. **Intraday-Kerzen** – der Parameter `TimeFrame` definiert die Arbeitsauflösung (Standard: 1 Minute). Each finished candle feeds
the EMA trend filter, updates the most recent support/resistance reference taken `NumBars` bars ago, and triggers the trading
Logik.
2. **Tägliche Kerzen** – ein spezielles Abonnement sammelt Daten über einen längeren Zeitraum. Zwei vom Benutzer konfigurierbare Indizes bestimmen das Swing-Hoch
und Swing Low, die als Anker für das Fibonacci-Gitter dienen. Immer wenn eine neue Tageskerze eintrifft, wird die gesamte Retracement-Leiter angezeigt
neu berechnet, einschließlich der Verlängerungen (161,8 %, 261,8 %, 423,6 %).

## Signalerzeugung
Der MQL-Berater speicherte den zuletzt entdeckten Swing-Hoch/Tief-Wert und ermittelte, welcher Swing zuerst auftrat (`highFirst`). Der Port behält die
same concept by comparing the day indices:
- If the selected high is more recent than the selected low (`highFirst = true`) the market is treated as descending and the
Fibonacci levels are measured upward from the low.
- Andernfalls wird die Bewegung als aufsteigend betrachtet und das Raster wird vom Hoch nach unten projiziert.

Für jede abgeschlossene Intraday-Kerze spiegeln die folgenden Regeln das Original EA wider:
1. **Trendfilter** – ein EMA mit dem Zeitraum `MaPeriod` klassifiziert die kurzfristige Tendenz. If the close price (treated as both bid and ask)
is above the EMA the trend is "Naik" (up); if it is below, the trend is "Turun" (down). When the price hovers exactly around the
EMA no trade will be opened.
2. **Fibonacci-Signal** – abhängig von `highFirst` erzeugt die Preisinteraktion mit den Niveaus 23,6 %, 76,4 %, 91 % und 14,6 % eines von
vier String-Signale aus dem MT4-Code: `Reverse-Buy`, `Reverse-Sell`, `Trading-Area` oder `Continuation`. Nur die ersten drei sind es
used for actual entries, the last one simply reports a trend continuation.
3. **Entry rules** – the original script contained six entry branches. They are reproduced verbatim:
   - Aufwärtstrend + Handelsbereich + Ausbruch über den Referenzwiderstand → Kauf mit dem Schutzstopp bei der referenzierten Unterstützung.
   - Aufwärtstrend + umgekehrter Verkauf + `highFirst == false` + Preis immer noch unter dem Widerstand → Eröffnen Sie einen Short mit dem Stop bei 14,6 %.
   - Aufwärtstrend + umgekehrter Kauf + `highFirst == false` + Preis unter dem Widerstand → Kauf mit dem Stop bei 91 %.
   - Abwärtstrend + Handelsbereich + Durchbruch unter Unterstützung → Verkauf mit Stopp an der Widerstandslinie.
   - Abwärtstrend + umgekehrter Verkauf + `highFirst == true` + Preis unter dem Widerstand → Verkauf mit dem Stop bei 91 %.
   - Abwärtstrend + umgekehrter Kauf + `highFirst == true` + Preis unter dem Widerstand → Kauf mit dem Stop bei 14,6 %.
Only one position may exist at a time; Aktive Aufträge werden nicht gestapelt.

## Positionsmanagement
- **Unterstützungs-/Widerstandsausgänge** – wie im EA wird eine Long-Position aufgelöst, wenn der Preis während a auf die Unterstützungsreferenz zurückfällt
Der Short wird geschlossen, wenn der Preis auf die Widerstandsreferenz steigt, unabhängig vom aktuellen Gewinn.
- **Anfänglicher Schutzstopp** – der während der Eintrittsentscheidung berechnete Stopppegel wird intern gespeichert und als Ausstiegsauslöser verwendet.
Die StockSharp-Version führt bei jeder Kerze die gleiche Prüfung durch, anstatt Brokeraufträge direkt zu ändern.
- **Stepped trailing stop** – the MQL script raised the stop level every 20 points after an initial 60-point move (e.g., +60 → stop
bis +55, +80 → Stopp bis +75, … bis +260). Der Port behält die genaue Rangliste bei, indem er das Instrument `PriceStep` verwendet, um Punkte in umzuwandeln
Preisverrechnungen. For short trades the stop slides downward to lock in profits, guaranteeing the same distance as the original.

## Parameter
| Name | Beschreibung | Standard | Notizen |
| --- | --- | --- | --- |
| `NumBars` | Verschiebung der Kerze, deren Hoch/Tief zum vorübergehenden Widerstand/Unterstützung wird. | `3` | Entspricht der externen Eingabe `numBars`; muss größer als Null sein. |
| `MaPeriod` | Zeitraum des EMA, der für die Trendklassifizierung verwendet wird. | `5` | Entspricht `maPeriod` im EA. |
| `TimeFrame` | Intraday candle timeframe. | `1 minute` | Mirrors the `timeFrame` extern; akzeptiert alle `TimeSpan`. |
| `DaysBackForHigh` | Index der täglichen Kerze, die das Swing-Hoch liefert. | `1` | Entspricht `daysBackForHigh`. |
| `DaysBackForLow` | Index der täglichen Kerze, die das Swing-Tief liefert. | `1` | Entspricht `daysBackForLow`. |
| `Volume` | Größe der Marktorder. | `1` | Stellt Grundstücke/Aktien dar; validated to stay positive. |

## Implementierungshinweise
- Das Original EA erstellte zahlreiche grafische Objekte. Diese Aufrufe werden absichtlich weggelassen, da StockSharp die Diagrammerstellung übernimmt
separately and the shapes were purely cosmetic.
- Instead of querying historical buffers like `iLow` and `iHigh`, the port maintains two in-memory lists of finished candles and
reads the required shift directly from there.
- Die Stop-Verwaltung wird im Strategiecode (`ManagePosition`) und nicht über `OrderModify` implementiert, wodurch der Verhaltensbroker erhalten bleibt
agnostisch unter Beibehaltung des gleichen Entscheidungsbaums.
- Auftragsablehnungen löschen den ausstehenden Eingabestatus, sodass manuelle Anpassungen keine veralteten internen Flags hinterlassen, die mit der Defensive übereinstimmen
Codierung, die in vielen bestehenden API-Strategien vorhanden ist.

## Unterschiede zur MetaTrader-Version
- MetaTrader hat Zugriff auf die Tick-Level `Ask` und `Bid` angenommen. StockSharp arbeitet standardmäßig mit Kerzenschlüssen; Es wird der Schlusskurs verwendet
als Bid- und Ask-Proxy, was zur Nachbildung der Entscheidungslogik ausreicht.
- The notion of "which extremum appeared first" cannot rely on MT4's `High[]`/`Low[]` series. Der Port nähert sich dem Wert durch Vergleich an
Die ausgewählten Tagesindizes liefern identische Ergebnisse für die Standardkonfiguration und behalten das beabsichtigte Verhalten für bei
andere Einstellungen.
- Brokerseitige Stop- und Take-Profit-Orders werden durch virtuelle Exits ersetzt, die pro Kerze ausgewertet werden. Dadurch wird eine steckerspezifische Reihenfolge vermieden
Typen und stellen gleichzeitig sicher, dass die gleichen Exit-Bedingungen erfüllt sind.
