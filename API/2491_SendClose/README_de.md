# SendClose-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
SendClose ist eine fraktalbasierte Ausbruchsstrategie, die das Verhalten des ursprünglichen MT5 Expert Advisors nachbildet. Der Algorithmus erstellt kontinuierlich dynamische Unterstützungs- und Widerstandslinien, indem er abwechselnde Fraktal-Pivots verbindet, und reagiert in dem Moment, wenn der Preis diese projizierten Niveaus erneut erreicht. Der StockSharp-Port behält die Kernmechaniken intakt: Trendlinien werden aus abwechselnden Auf-/Ab-Fraktalsequenzen generiert, Ausbrüche lösen Markteinstiege aus, und separate Offset-Linien werden verwendet, um die Positionsliquidation zu erzwingen.

## Fraktal-Erkennungsworkflow
1. **Fünf-Kerzen-Fenster** – die Strategie hält einen rollenden Puffer der letzten fünf abgeschlossenen Kerzen. Sobald das Fenster voll ist, wertet sie die mittlere Kerze gegen die zwei älteren und zwei neueren Nachbarn aus.
2. **Aufwärts-Fraktal-Regel** – die mittlere Kerze bildet ein Aufwärts-Fraktal, wenn ihr Hoch größer ist als die Hochs der zwei neueren Kerzen und streng größer als die Hochs der zwei älteren Kerzen. Dies entspricht der MT5 `iFractals`-Logik (>= auf der neueren Seite, > auf der älteren Seite).
3. **Abwärts-Fraktal-Regel** – entsprechend ist die mittlere Kerze ein Abwärts-Fraktal, wenn ihr Tief kleiner oder gleich im Vergleich mit den neueren Kerzen und streng kleiner als die zwei älteren Kerzen ist.
4. **Fraktal-Warteschlange** – jedes neu bestätigte Fraktal wird in eine Sechs-Element-FIFO-Warteschlange eingereiht, geordnet von neuesten zu ältesten. Diese Warteschlange wird später gescannt, um die erforderlichen alternierenden Muster zu finden.

## Trendlinienkonstruktion
* **Verkaufslinie** – der Algorithmus sucht nach der aktuellsten Sequenz *Aufwärts-Fraktal → Abwärts-Fraktal → Aufwärts-Fraktal*. Die Linie wird durch das erste und letzte Aufwärts-Fraktal gezogen und verbindet effektiv zwei Swing-Hochs, die durch ein Swing-Tief getrennt sind.
* **Kauflinie** – symmetrisch sucht er nach einer *Abwärts-Fraktal → Aufwärts-Fraktal → Abwärts-Fraktal*-Kette und verbindet die umgebenden Swing-Tiefs.
* **Projektion** – die gespeicherten Endpunkte (Zeit und Preis) werden verwendet, um den Linienwert für jeden späteren Zeitstempel zu interpolieren oder zu extrapolieren. Wenn der Markt die Projektion beim aktuellen Kerzenschluss erreicht, wird eine Handelsentscheidung getroffen.
* **Schließlinien** – zwei Hilfsniveaus werden berechnet, indem die Verkaufslinie nach oben und die Kauflinie nach unten um `LineOffsetSteps * PriceStep` verschoben wird. Sie fungieren als erzwungene Exit-Trigger genau wie die ursprünglichen Close1/Close2-Linien.

## Handelslogik
* **Einstiegsbedingungen**
  * Verkaufen wenn der Preis die Verkaufslinie berührt und kein widersprüchliches Long-Engagement vorhanden ist. Bestehendes Short-Engagement kann erhöht werden, bis das `MaxPositions`-Limit erreicht ist.
  * Kaufen wenn der Preis die Kauflinie berührt und kein widersprüchliches Short-Engagement vorhanden ist. Bestehendes Long-Engagement kann bis zum gleichen Limit erhöht werden.
* **Ausstiegsbedingungen**
  * Preis, der eine Schließlinie berührt, schließt sofort die offene Position und emuliert das MT5-Verhalten, bei dem das Berühren von Close1/Close2 einen vollständigen Exit auslöst.
  * Einstiegssignale versuchen, entgegengesetzte Positionen zu glätten, bevor die neue Order platziert wird, und spiegeln die Hedging-zu-Netting-Anpassung innerhalb von StockSharp wider.
* **Touch-Erkennung** – die Tick-Präzision von MT5 wird mit Kerzendaten approximiert. Ein Niveau gilt als „berührt", wenn es zwischen dem Kerzenhoch und -tief liegt.

## Parameter
| Name | Beschreibung |
|------|-------------|
| `EnableSellLine` | Aktiviert oder deaktiviert Orders basierend auf der oberen (Verkaufs-)Fraktallinie. |
| `EnableBuyLine` | Aktiviert oder deaktiviert Orders basierend auf der unteren (Kauf-)Fraktallinie. |
| `EnableCloseSellLine` | Schaltet das Close1-Niveau um, das Positionen schließt, wenn der Preis über die Verkaufslinie plus Offset steigt. |
| `EnableCloseBuyLine` | Schaltet das Close2-Niveau um, das Positionen schließt, wenn der Preis unter die Kauflinie minus Offset fällt. |
| `MaxPositions` | Maximale Anzahl von Lots, die in einer Richtung offen bleiben dürfen. Zusätzliche Einstiege über diese Grenze hinaus werden ignoriert. |
| `OrderVolume` | Volumen jeder Marktorder. Der Wert sollte der Kontraktgröße des Instruments entsprechen. |
| `LineOffsetSteps` | Offset, gemessen in Preisschritten, bei der Berechnung von Close1/Close2-Niveaus. Der Standard 15 repliziert die `15*Point()`-Verschiebung aus MT5. |
| `CandleType` | Kerzenserie für die Analyse. Wählen Sie einen Zeitrahmen, der dem Chart entspricht, den Sie handeln möchten (z.B. M15, H1). |

## Implementierungshinweise
* Die Strategie läuft auf abgeschlossenen Kerzen, um den ursprünglichen EA zu respektieren, der auf bestätigte MT5-Bars angewiesen war, bevor Fraktale ausgewertet wurden.
* Tick-Level-Gleichheit mit Bid/Ask wird mit Kerzenbereichen approximiert. Wenn höhere Präzision erforderlich ist, Tick-Daten statt Kerzen einspeisen.
* Der `MaxPositions`-Parameter operiert auf der Netto-StockSharp-Position. Er eignet sich daher für Netting-Konten; Hedging-Konten können das Skalieren durch Erhöhung von `MaxPositions` simulieren.
* Schließlinien werden vor Einstiegen ausgewertet. Wenn sowohl ein Exit als auch ein Einstieg auf derselben Kerze ausgelöst werden, hat der Exit Vorrang, um widersprüchliche Orders zu vermeiden.

## Verwendungsrichtlinien
1. Gewünschtes Symbol und Zeitrahmen im StockSharp-Terminal konfigurieren und sicherstellen, dass das Instrument `PriceStep`-Informationen bereitstellt. Die Offset-Logik ist darauf angewiesen.
2. `CandleType` anpassen, um dem zu analysierenden Zeitrahmen zu entsprechen. Der Standard ist 30 Minuten, was ein Gleichgewicht zwischen Rauschen und Reaktionsfähigkeit bietet.
3. `OrderVolume` auf die Positionsgröße setzen, die pro Trade gesendet werden soll. Für Futures Kontraktzählungen verwenden; für FX-CFDs Lotgrößen verwenden.
4. `LineOffsetSteps` auf die Volatilität des Instruments abstimmen. Größere Offsets erfordern eine stärkere Bewegung, um die Close1/Close2-Exits auszulösen.
5. Anzahl der offenen Lots überwachen, wenn `MaxPositions` erhöht wird. Die Strategie überschreitet diese Grenze nicht, kann aber in Trendmärkten Positionen pyramidisieren.

## Unterschiede zur MT5-Version
* StockSharp operiert mit Netto-Positionen, daher gleicht der Code entgegengesetztes Engagement aus, bevor ein neuer Trade geöffnet wird, anstatt simultane Kauf-/Verkaufstickets zu halten.
* Grafikobjekte werden nicht automatisch gezeichnet. Wenn On-Chart-Visualisierung benötigt wird, ein Chart-Modul verbinden und die generierten Linienwerte manuell plotten.
* Kanalbasierte Touch-Erkennung kann etwas später als MT5-Tick-Checks feuern, besonders bei schnellen Märkten mit breiten Kerzen.

## Risikomanagement
Die Strategie platziert Marktorders ohne integrierte Stop-Losses. Immer mit externen Risikokontrollen wie Eigenkapital-Stops, Handelszeitfiltern oder manueller Aufsicht ergänzen. Vor dem Live-Einsatz ausführlich auf dem Zielinstrument und -zeitrahmen backtesten.
