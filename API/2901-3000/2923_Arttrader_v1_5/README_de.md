# Arttrader v1.5-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Arttrader v1.5-Strategie ist ein Trendfolgesystem, das vom ursprünglichen MetaTrader 5-Expertenberater konvertiert wurde. Es kombiniert einen EMA-Neigungsfilter (exponentieller gleitender Durchschnitt) des höheren Zeitrahmens mit einem kurzfristigen Preisaktions-Einstiegsmodell. Die StockSharp-Version behält das Risikomanagementverhalten des Quellcodes bei, einschließlich der strengen Handhabung großer Kerzen-Gaps, Zeitfenstern für Orders und Notfallausstiegen basierend auf der Preisentfernung.

Zwei Kerzenströme werden gleichzeitig verwendet:

- **Trading-Kerzen** (standardmäßig 5-Minuten) generieren Einstiege, Ausstiege und alle preisbasierten Filter.
- **Trend-Kerzen** (standardmäßig 1-Stunde) speisen den EMA, der die Neigung des höheren Zeitrahmens-Trends misst.

Die Strategie handelt ein einzelnes Instrument mit Nettopositionen. Wenn ein entgegengesetztes Signal erscheint, wird das bestehende Engagement flachgestellt und eine neue Marktorder in der Signalrichtung eingereicht.

## Signallogik
1. **EMA-Neigungsfilter**
   - Der stündliche EMA des Kerzen-Eröffnungspreises muss eine Neigung zwischen `SlopeSmall` und `SlopeLarge` aufweisen (umgerechnet in Preiseinheiten durch den Instrument-Punktwert).
   - Long-Trades erfordern eine positive Neigung, Short-Trades erfordern eine negative Neigung.
2. **Intrabar-Timing**
   - Signale werden erst berücksichtigt, nachdem `MinutesBegin` Minuten in der aktuellen Stunde verstrichen sind, was der MT5-`TimeCurrent()`-Prüfung entspricht.
3. **Preisaktionsbestätigung**
   - Long-Einstiege benötigen eine bärische oder neutrale Kerze, die nahe ihrem Tief schließt (`SlipBegin` definiert die akzeptable Distanz).
   - Short-Einstiege benötigen eine bullische oder neutrale Kerze, die nahe ihrem Hoch schließt.
4. **Sprungfilter**
   - Ein einzelner Kerzen-Eröffnungs-Gap größer als `BigJump` (in angepassten Punkten) innerhalb der letzten sechs Kerzen bricht sowohl Long- als auch Short-Signale ab.
   - Ein Zwei-Kerzen-Eröffnungs-Gap größer als `DoubleJump` bricht das Signal ebenfalls ab, um Trades während volatiler Spikes zu verhindern.

## Ausstiegslogik
1. **Zeitlich gesteuerter intelligenter Stop**
   - Ein Referenz-Einstiegspreis wird mit einem optionalen `Adjust`-Offset gespeichert, um das MT5-Spread-Handling zu emulieren.
   - Wenn der Schlusskurs gegen die Position um mindestens `StopLoss` läuft, wartet die Strategie, bis `MinutesEnd` Minuten der Stunde vergangen sind und die Kerze ein Erholungsmuster zeigt (`SlipEnd`-Anforderung). Sobald erfüllt, wird die Position zum Marktpreis geschlossen.
2. **Notfall-Stop**
   - Wenn der Kerzenbereich `EmergencyLoss` vom aufgezeichneten Füllpreis entfernt berührt, wird die Position sofort geschlossen. Dies spiegelt den broker-seitigen Stop-Loss des ursprünglichen Experten wider.
3. **Take-Profit**
   - Eine Kerze, die die `TakeProfit`-Distanz berührt, löst einen sofortigen Ausstieg aus.
4. **Volumen-Sicherheitsnetz**
   - Wenn das Gesamtvolumen der vorherigen Kerze `MinVolume` nicht übersteigt, wird die aktuelle Position geschlossen, um Handel in illiquiden Perioden zu vermeiden.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|-------------|
| `Volume` | 1 | Marktorder-Volumen. Wird sowohl für Einstiege als auch zum Umdrehen einer entgegengesetzten Position verwendet. |
| `EmaPeriod` | 11 | Länge des EMA, berechnet auf dem Trend-Zeitrahmen (Eröffnungspreisquelle). |
| `BigJump` | 30 | Maximal erlaubter einzelner Kerzen-Gap zwischen aufeinanderfolgenden Eröffnungen (umgerechnet mit Preisschritt). |
| `DoubleJump` | 55 | Maximal erlaubter Gap zwischen Eröffnungen, die eine Kerze auseinanderliegen. |
| `StopLoss` | 20 | Verlust in Punkten, der die zeitgesteuerte Ausstiegslogik aktiviert. |
| `EmergencyLoss` | 50 | Harter Stop-Abstand vom Einstieg, sofort ausgeführt wenn erreicht. |
| `TakeProfit` | 25 | Gewinnzielbstand vom Einstieg. |
| `SlopeSmall` | 5 | Minimale EMA-Neigung (positiv für Longs, negativ für Shorts), die für neue Trades erforderlich ist. |
| `SlopeLarge` | 8 | Maximale erlaubte EMA-Neigungsgröße für Trades. |
| `MinutesBegin` | 25 | Minuten nach der vollen Stunde, bevor neue Einstiege ausgewertet werden. |
| `MinutesEnd` | 25 | Minuten nach der vollen Stunde, bevor die zeitgesteuerte Stop-Logik aussteigen kann. |
| `SlipBegin` | 0 | Maximale Distanz zwischen Kerzenschluss und dem Extremum bei der Einstiegsvalidierung. |
| `SlipEnd` | 0 | Maximale Distanz zwischen Kerzenschluss und dem Extremum bei der Stop-Bestätigung. |
| `MinVolume` | 0 | Mindestvolumen der vorherigen Kerze; niedrigere Werte erzwingen einen Ausstieg. |
| `Adjust` | 1 | Anpassung beim Speichern des internen Einstiegsreferenzpreises. |
| `CandleType` | 5-Minuten-Zeitrahmen | Trading-Kerzen für Einstiege und Ausstiege. |
| `TrendCandleType` | 1-Stunden-Zeitrahmen | Kerzentyp, der den EMA-Neigungsfilter speist. |

Alle preisbasierten Parameter werden mit dem Instrument-Punktwert multipliziert. Für FX-Symbole mit drei oder fünf Dezimalstellen multipliziert der Code den Punkt automatisch mit zehn, entsprechend der Pip-Handhabung in der MetaTrader-Version.

## Implementierungshinweise
- Beide Markteinstiegsmethoden rufen `BuyMarket` oder `SellMarket` mit genügend Volumen auf, um eine bestehende Position bei Bedarf umzukehren.
- Die Strategie verwendet `SubscribeCandles` nur dann zweimal, wenn sich Trading- und Trend-Kerzentypen unterscheiden. Wenn beide Parameter gleich sind, speist ein einzelnes Abonnement sowohl den EMA als auch die Trade-Logik.
- Notfall-Stop und Take-Profit-Management werden prozessintern implementiert, da StockSharp Schutzorders nicht automatisch an Marktausführungen anhängt.
- Die High-Level-API wird durchgehend verwendet (`Bind`-Abonnements, `StartProtection`, Chart-Helfer), um den Code prägnant zu halten und Repository-Konventionen zu folgen.

## Verwendungshinweise
- Passen Sie `MinutesBegin` und `MinutesEnd` für Instrumente mit unterschiedlichen Session-Strukturen an. Die Standardwerte sind für Instrumente mit stündlichem Rhythmus wie wichtige Forex-Paare ausgelegt.
- Erhöhen Sie `MinVolume` auf Märkten, wo plötzliche Volumenausfälle mit schlechten Füllungen zusammenfallen (z. B. Rohstoffe außerhalb der Pit-Stunden).
- Da Sprungfilter nur auf sechs Kerzen angewiesen sind, stellen Sie sicher, dass der Trading-Zeitrahmen nicht zu groß ist; andernfalls kann der Filter zu permissiv sein.
- Der EMA-Neigungsfilter ist empfindlich gegenüber dem Instrument-Punktwert. Überprüfen Sie immer, ob `BigJump`, `StopLoss` und ähnliche Parameter für das ausgewählte Symbol korrekt skaliert sind.
