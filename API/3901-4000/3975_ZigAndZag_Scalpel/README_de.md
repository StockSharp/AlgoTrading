# ZigAndZag-Skalpell-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
ZigAndZagScalpelStrategy ist ein StockSharp-Port des MetaTrader 4 „ZigAndZag“-Toolkits (Ordner 8304).
Das Originalpaket kombiniert einen benutzerdefinierten Indikator und einen Expertenberater. Es werden zwei ZigZag-Fenster verwendet:

* **KeelOver** – ein Long-Lookback-Swing-Detektor, der den vorherrschenden Trend markiert.
* **Slalom** – ein kurzer Lookback-Swing-Detektor, der umsetzbare Ausbrüche definiert.

Wenn sich der langfristige ZigZag nach oben dreht, sucht die Strategie nach dem nächsten Slalom-Tief und wartet auf den Preis
um eine konfigurierbare Anzahl von Punkten über diesen Pivot zu steigen. Eine Marktkauforder wird erteilt, sobald die
Ausbruchsdistanz erreicht ist. Eine symmetrische Regel eröffnet eine Short-Position, wenn sich der KeelOver-Trend dreht
Nach unten markiert der Slalom ein neues Hoch und der Preis fällt darunter. Positionen können optional geschlossen werden
Sobald der entgegengesetzte Slalom-Drehpunkt bestätigt wird, wird die Entfernung des Grenzpfeils des Indikators nachgeahmt.

Die Implementierung hält den täglichen Handelslimiter vom Fachberater fern. Nur eine konfigurierbare Nummer
Anzahl der Trades ist pro Handelstag zulässig und wird automatisch um Mitternacht (Börsenzeit) zurückgesetzt. Dies
reproduziert die Flagge „Neuer Tag“ aus dem Originalcode.

## Wie es funktioniert
1. Abonnieren Sie den durch `CandleType` definierten primären Kerzenstream.
2. Füttere zwei `ZigZagIndicator`-Instanzen:
   * Tiefe = `KeelOverLength` für den Trenddetektor.
   * Tiefe = `SlalomLength` für Eintrittssignale.
3. Verfolgen Sie den letzten KeelOver-Pivot, um festzustellen, ob der Trend nach oben zeigt (der letzte Pivot ist ein Tief).
oder nach unten (letzter Pivot ist ein Hoch).
4. Wenn der Slalom-Indikator einen neuen Pivot meldet, aktivieren Sie einen Ausbruch in diese Richtung.
5. Berechnen Sie den gewichteten Preis `(5×Close + 2×Open + High + Low) / 9`. Wenn sich der Preis um mehr als bewegt
`BreakoutDistancePoints` (umgerechnet in Preiseinheiten) vom Pivot entfernt, während der Trend unterstützt
Wenn Sie sich bewegen, führen Sie eine Market-Order aus.
6. Schließen Sie bestehende Positionen, wenn der globale Trend umkehrt oder der entgegengesetzte Slalom-Pivot erscheint und
`CloseOnOppositePivot` ist aktiviert.
7. Setzen Sie den täglichen Handelszähler bei jedem Kalendertagswechsel zurück.

Die Parameter `DeviationPoints` und `Backstep` werden von beiden ZigZag-Instanzen gemeinsam genutzt, sodass die
Die Swing-Struktur entspricht den MetaTrader-Indikatorpuffern.

## Parameter
| Name | Standard | Beschreibung |
| ---- | ------- | ----------- |
| `CandleType` | `15m` | Primärer Zeitrahmen für den Bau beider ZigZag-Leitern. |
| `KeelOverLength` | `55` | Langfristiger ZigZag-Lookback, der den Trend definiert (ursprünglich `KeelOver`). |
| `SlalomLength` | `17` | Kurzfristiger ZigZag-Lookback, der für Einträge verwendet wird (ursprünglich `Slalom`). |
| `DeviationPoints` | `5` | Mindestschwunggröße in Punkten, bevor ein neuer ZigZag-Pivot bestätigt wird. |
| `Backstep` | `3` | Erforderlicher Stangenabstand zwischen aufeinanderfolgenden Drehpunkten. |
| `BreakoutDistancePoints` | `2` | Entfernung von einem Drehpunkt (in Punkten), bevor ein Befehl abgegeben wird. |
| `MaxTradesPerDay` | `1` | Maximale Anzahl Einträge pro Kalendertag. Spiegelt die ursprüngliche `newday`-Flagge wider. |
| `CloseOnOppositePivot` | `true` | Schließen Sie offene Positionen, wenn der Slalom ZigZag den entgegengesetzten Schwung erzeugt. |

Alle punktbasierten Parameter werden mit `Security.PriceStep` in Preiseinheiten umgewandelt. Wenn das Instrument
Ist keine Preisstufe konfiguriert, wird ein Wert von `1` verwendet, um die Strategie während des Tests funktionsfähig zu halten.

## Nutzungshinweise
* Die Strategie arbeitet mit Marktaufträgen (`BuyMarket` / `SellMarket`). Fügen Sie Ihre eigenen Risikoregeln hinzu
oder Stop-Loss-Helfer, wenn ein strengeres Risikomanagement erforderlich ist.
* Da beide ZigZag-Indikatoren denselben Kerzenstrom teilen, stellen Sie sicher, dass der ausgewählte `CandleType` ist
wird von Ihrem Datenadapter unterstützt.
* `MaxTradesPerDay = 1` reproduziert das Verhalten „ein Trade pro Tag“. Erhöhen Sie den Wert bei Bedarf
mehrere Einträge während derselben Sitzung.
* Legen Sie `CloseOnOppositePivot = false` fest, um Positionen offen zu halten, bis sich der globale Trend umkehrt
auf jede kurzfristige Schwankung reagieren.

## Unterschiede zum MT4-Expertenberater
* Die MetaTrader-Version hat Pfeile für ausstehende Grenzwerte platziert. Der Port StockSharp führt Breakouts mit aus
sofortige Marktaufträge, um innerhalb des hohen Niveaus API zu bleiben.
* Auf Risikomanagement, Losgrößenbestimmung und Teilabschlüsse wird bewusst verzichtet. Verwenden Sie die Position StockSharp
Dimensionierungshilfen, wenn Sie eine erweiterte Kapitalkontrolle benötigen.
* Die Indikatorpuffer 4/5/6 werden durch direkte Strategielogik und Diagrammanmerkungen über ersetzt
`DrawIndicator` und `DrawOwnTrades`.

## Empfohlene Erweiterungen
* Fügen Sie Stop-Loss- und Take-Profit-Parameter hinzu, die an ATR oder aktuelle ZigZag-Schwankungen gebunden sind.
* Überlagern Sie den ursprünglichen Indikator mit `BreakoutDistancePoints = 0`, um die rohe Pivot-Leiter zu visualisieren.
* Kombinieren Sie es mit einem Sitzungsfilter (`IsFormedAndOnlineAndAllowTrading`), um die Handelszeiten zu begrenzen.
