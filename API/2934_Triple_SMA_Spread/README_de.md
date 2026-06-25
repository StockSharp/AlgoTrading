# Triple SMA Spread-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist ein C#-Port des MetaTrader 5-Expertenberaters `3sma.mq5` (id 21495). Sie verfolgt dieselbe Idee, zu handeln, wenn sich drei einfache gleitende Durchschnitte um einen konfigurierbaren Spread voneinander trennen. Die Implementierung verwendet die StockSharp-High-Level-API mit Kerzen-Abonnements und Indikator-Bindung, sodass keine manuelle Serienverwaltung erforderlich ist.

## Ursprüngliches MT5-Verhalten
Der MT5-Experte basiert auf drei einfachen gleitenden Durchschnitten mit verschiedenen Perioden und Anzeigeverschiebungen. Der schnelle Durchschnitt verwendet den aktuellen Balken, während der mittlere und langsame Durchschnitt um einen und zwei Balken in die Vergangenheit verschoben sind. Bei jedem Tick:

1. Konvertiert den benutzerdefinierten Spread von Pips in Preiseinheiten basierend auf der Symbolpräzision.
2. Schließt Long-Positionen, wenn der schnelle SMA um mindestens die Hälfte des Spreads unter den mittleren SMA fällt, und schließt Short-Positionen, wenn der schnelle SMA um die Hälfte des Spreads über den mittleren SMA steigt.
3. Öffnet neue Long-Positionen, wenn `MA1 > MA2 + spread` und `MA2 > MA3 + spread`, solange keine anderen Long-Trades des Experten bestehen. Analog öffnet er Short-Positionen, wenn alle drei Durchschnitte in der entgegengesetzten Reihenfolge ausgerichtet sind.
4. Verwendet nur Marktorders mit einer festen Losgröße und wendet keine expliziten Stop-Loss- oder Take-Profit-Level an.

## StockSharp-Implementierung
* Indikatoren – drei `SimpleMovingAverage`-Instanzen abonnieren dieselbe Kerzenquelle. Kompakte Verlaufspuffer reproduzieren die MT5-"Shift"-Parameter, sodass jeder Vergleich fertige Balkenwerte aus den angeforderten Offsets verwendet.
* Spread-Handhabung – der Spread-Parameter wird in Pips eingegeben. Die Strategie leitet eine Pip-Größe aus `Security.PriceStep` (oder `Security.Step`) ab und multipliziert sie mit zehn für 3/5-stellige FX-Symbole, was der MT5-Anpassung für Bruchteils-Quotes entspricht.
* Order-Fluss – Orders werden mit `BuyMarket`/`SellMarket` eingereicht. Wenn eine Umkehrbedingung erscheint, fügt die Strategie den Absolutwert der aktuellen Nettoposition zum Basisvolumen hinzu, um das entgegengesetzte Exposure aufzulösen und die neue Richtung mit einer einzigen Marktorder zu etablieren.
* Visualisierung – wenn Charts verfügbar sind, zeichnet die Strategie die Quellkerzen zusammen mit den drei gleitenden Durchschnitten und ausgeführten Trades.

## Parameter
| Name | Beschreibung | Standard |
|------|--------------|----------|
| `Volume` | Ordervolumen für jeden Markteinstieg. | `0.1` |
| `FastMaPeriod` | Periode des schnellen SMA (entspricht MA1 in MT5). | `9` |
| `FastMaShift` | Anzahl der fertigen Balken zur Verschiebung des schnellen SMA. | `0` |
| `MiddleMaPeriod` | Periode des mittleren SMA (MA2). | `14` |
| `MiddleMaShift` | Verschiebung in fertigen Balken für den mittleren SMA. | `1` |
| `SlowMaPeriod` | Periode des langsamen SMA (MA3). | `29` |
| `SlowMaShift` | Verschiebung in fertigen Balken für den langsamen SMA. | `2` |
| `MaSpreadPips` | Minimal erforderlicher Spread zwischen aufeinanderfolgenden SMAs in Pips. | `10` |
| `CandleType` | Kerzenserie für Berechnungen. | `1 Minuten`-Zeitrahmen |

## Handelslogik
1. Warten bis alle drei gleitenden Durchschnitte gebildet sind und die Verlaufspuffer Werte für die angeforderten Verschiebungen enthalten.
2. Den Spread-Parameter von Pips in Preiseinheiten konvertieren und den Halb-Spread für Exit-Filter berechnen.
3. **Exit-Filter** –
   * Long-Exposure schließen, wenn der verschobene schnelle SMA um mindestens die Hälfte des Spreads unter den verschobenen mittleren SMA fällt.
   * Short-Exposure schließen, wenn der verschobene schnelle SMA um mindestens die Hälfte des Spreads über den verschobenen mittleren SMA steigt.
4. **Eintrittsbedingungen** –
   * Long einsteigen (oder von Short zu Long umkehren), wenn der schnelle SMA größer als der mittlere SMA plus Spread **und** der mittlere SMA größer als der langsame SMA plus Spread ist.
   * Short einsteigen (oder von Long zu Short umkehren), wenn der schnelle SMA kleiner als der mittlere SMA minus Spread **und** der mittlere SMA kleiner als der langsame SMA minus Spread ist.

## Unterschiede zur MT5-Version
* StockSharp arbeitet mit einer einzigen Nettoposition pro Wertpapier. Wenn ein Umkehrsignal erscheint, gibt die Strategie eine einzige Marktorder aus, die so dimensioniert ist, dass sie das vorherige Netto-Exposure auflöst und die neue Richtung etabliert. Der MT5-Experte konnte unabhängige Long- und Short-Positionen halten.
* Die Pip-Konvertierung verwendet die besten verfügbaren `Security`-Metadaten. Wenn der Broker weder `PriceStep` noch `Step` bereitstellt, wird `1` als Fallback verwendet.
* Orders werden auf fertigen Kerzen statt bei jedem Tick eingereicht, da die High-Level-API auf Kerzen-Abonnements basiert.
* Die Strategie implementiert nicht die ausführlichen Protokollierungshelfer aus dem MT5-Code; StockSharp-integrierte Protokollierung kann bei Bedarf verwendet werden.

## Nutzungshinweise
* Sicherstellen, dass die ausgewählte Kerzenserie dem in der ursprünglichen MT5-Einrichtung verwendeten Zeitrahmen entspricht.
* Den Spread-Parameter anpassen, wann immer das Instrument nicht standardmäßige Pip-Größen verwendet.
* Da die Strategie mit fertigen Kerzen arbeitet, wird die Ausführung verzögert, bis die aktuelle Kerze schließt.
