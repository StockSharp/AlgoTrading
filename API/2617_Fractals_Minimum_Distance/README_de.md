# Fractals Mindestabstand
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Fractals Mindestabstand repliziert den MetaTrader Expert Advisor "Fractals minimum distance" mit der High-Level-Strategie-API von StockSharp. Das System durchsucht die konfigurierte Kerzenserie nach Fünf-Balken-Fraktalmustern im Stil von Bill Williams. Jedes Mal, wenn ein neues bestätigtes Fraktal beim angegebenen Signalbalken-Offset erscheint, misst die Strategie den Abstand zwischen den zuletzt aufgetretenen Auf- und Abwärtsfraktalen. Ein Marktauftrag ist nur erlaubt, wenn dieser Abstand den erforderlichen Schwellenwert in Pips überschreitet.

Die Konvertierung behält das ursprüngliche Verhalten bei, jegliche entgegengesetzte Exposition sofort vor der Umkehrung zu schließen. Im Gegensatz zur MQL-Version wird die Positionsgröße aus der `Volume`-Eigenschaft der Strategie entnommen, anstatt kontobasierte Risikoberechnungen durchzuführen. Es werden keine Stop-Loss- oder Take-Profit-Aufträge gesendet, was dem Quell-Expert entspricht.

## Signallogik
1. Den durch `CandleType` definierten Kerzentyp abonnieren und rollierende Puffer von Hochs und Tiefs erstellen, die immer die `SignalBar` Kerzen in der Vergangenheit liegende Kerze sowie zwei Nachbarn auf jeder Seite enthalten.
2. Ein **oberes Fraktal** erkennen, wenn das Hoch der Mittelkerze strikt größer ist als die Hochs der zwei vorausgehenden und der zwei folgenden Kerzen. Ein **unteres Fraktal** analog für Tiefs erkennen.
3. Den Parameter `DistancePips` mithilfe des `PriceStep` des Symbols in eine Preisdistanz umrechnen. Symbole mit drei oder fünf Dezimalstellen werden automatisch angepasst, um 0.001/0.00001-Notierungen als einen Pip zu behandeln.
4. Wenn ein oberes Fraktal bestätigt wird:
   - Das neue obere Niveau speichern und bestehende Long-Positionen schließen.
   - Wenn sowohl das letzte obere als auch das untere Fraktal bekannt sind und ihre absolute Differenz mindestens dem Distanzschwellenwert entspricht, einen Marktverkaufsauftrag mit `Volume` einreichen.
5. Wenn ein unteres Fraktal bestätigt wird:
   - Das neue untere Niveau speichern und bestehende Short-Positionen schließen.
   - Wenn die Distanzbedingung erfüllt ist, einen Marktkaufauftrag mit `Volume` einreichen.

Trades werden nur nach dem Schließen der Kerze platziert, die das Fraktal abschließt, um sicherzustellen, dass unfertige Kerzen niemals Einstiege auslösen. Die Strategie basiert auf `IsFormedAndOnlineAndAllowTrading()`, um das Platzieren von Aufträgen zu vermeiden, bevor die Umgebung bereit ist.

## Parameter
| Name | Beschreibung | Hinweise |
| --- | --- | --- |
| `DistancePips` | Mindestabstand zwischen dem letzten Auf- und Abwärtsfraktal in Pips gemessen. | Wird intern mithilfe der Tick-Größe des Instruments in Preiseinheiten umgerechnet. |
| `SignalBar` | Anzahl der vollständig geschlossenen Kerzen, die nach der das Fraktal enthaltenden Kerze vergehen müssen. | Effektiver Mindestwert ist 2, entsprechend der Zwei-Kerzen-Bestätigung der Bill Williams Fraktale. |
| `CandleType` | Datenserie, die die Berechnungen speist. | Standard ist der Ein-Minuten-Zeitrahmen; ändern Sie dies, um mit anderen Auflösungen zu arbeiten. |
| `Volume` | Standardmäßige StockSharp-Strategieeigenschaft zur Definition der Handelsgröße. | Ersetzt die ursprüngliche risikobasierte Dimensionierung des MetaTrader-Experten. |

## Positionsverwaltung und Unterschiede zu MQL
- Positionen werden immer vor der Richtungsumkehrung abgebaut, genau wie es der Quell-`ClosePositions`-Helfer tat.
- Der ursprüngliche Experte rief `RefreshRates()` auf und führte explizite Slippage-Einstellungen durch. Diese Aspekte werden in diesem Port an die StockSharp-Infrastruktur delegiert.
- Stop-Loss- und Take-Profit-Aufträge waren nicht Teil der MQL-Logik und fehlen hier weiterhin.
- `DistancePips` verwendet ganzzahlige Präzision wie der `ushort`-Input, während `SignalBar` den MQL-`uchar`-Input widerspiegelt.
- Da StockSharp mit Netto-Positionen arbeitet, kehrt das Öffnen einer Order in entgegengesetzter Richtung automatisch die Exposition um, was dem MetaTrader-Netting-Verhalten entspricht.

## Verwendungstipps
- Beginnen Sie mit demselben Signalbalken-Offset (`SignalBar = 3`) aus dem Originalcode und kalibrieren Sie den Distanzschwellenwert entsprechend der Volatilität des Instruments.
- Erhöhen Sie `SignalBar`, um mehr Kerzen nach dem Erscheinen eines Fraktals zu warten, was schnelle Schwankungen herausfiltern kann.
- Kombinieren Sie mit externem Risikomanagement wie dem integrierten `StartProtection()`-Helfer, wenn ein Schutz-Stop erforderlich ist.
