# Kleine Inside-Bar-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die Kleine Inside-Bar-Strategie sucht nach einem kompakten Inside-Bar-Muster, dem ein Momentum-Wechsel zwischen zwei aufeinanderfolgenden Kerzen folgt. Der ursprüngliche MetaTrader 5-Experte wurde in die StockSharp High-Level-API übersetzt und arbeitet jetzt ausschließlich auf abgeschlossenen Kerzen. Der Ansatz richtet sich an Trader, die Ausbruchs-Einstiege bevorzugen, die durch komprimierte Volatilitätsphasen ausgelöst werden.

## Musterdefinition
Die Strategie bewertet die zwei jüngsten abgeschlossenen Kerzen:

1. **Inside-Bar-Bedingung** – die zuletzt abgeschlossene Kerze muss vollständig im Bereich der vorherigen liegen.
2. **Range-Ratio-Filter** – der Bereich der Mutterbar (zwei Bars zurück) muss mindestens einem konfigurierbaren Vielfachen des Inside-Bar-Bereichs entsprechen. Das Standard-Verhältnis ist 2:1.
3. **Richtungsfilter** –
   - Ein Long-Setup erfordert eine bullische Inside-Bar, die sich in der unteren Hälfte der Mutterbar bildet, zusammen mit einer bärischen Mutterbar.
   - Ein Short-Setup erfordert eine bärische Inside-Bar, die sich in der oberen Hälfte der Mutterbar bildet, zusammen mit einer bullischen Mutterbar.
4. Optionale Umkehr tauscht die Long- und Short-Interpretationen, behält aber die gleichen geometrischen Anforderungen bei.

## Positionsverwaltung
Der Parameter `OpenMode` spiegelt das Verhalten des ursprünglichen EA wider:

- **AnySignal** – übermittelt bei jedem Signal eine neue Market-Order. Wenn eine entgegengesetzte Position vorhanden ist, wird sie teilweise ausgeglichen, da StockSharp Netting-Konten verwendet.
- **SwingWithRefill** – flacht die entgegengesetzte Exposition vor dem Einstieg ab und erlaubt mehrere Ergänzungen in dieselbe Richtung.
- **SingleSwing** – hält höchstens einen direktionalen Trade; neue Signale werden ignoriert, solange eine ausgerichtete Position offen ist.

Sowohl Long- als auch Short-Einstiege können unabhängig aktiviert werden. Reversal-Trading invertiert einfach, welches Setup Long- oder Short-Orders produziert.

## Parameter
| Name | Standard | Beschreibung |
|------|---------|--------------|
| `CandleType` | 1-Stunden-Zeitrahmen | Kerzen-Subscription zur Mustererkennung. |
| `RangeRatioThreshold` | 2.0 | Minimales Mutter-zu-Inside-Range-Verhältnis. |
| `EnableLong` | true | Bullische Trades erlauben. |
| `EnableShort` | true | Bärische Trades erlauben. |
| `ReverseSignals` | false | Long- und Short-Musterrichtungen tauschen. |
| `OpenMode` | SwingWithRefill | Steuert, wie bestehende Exposition bei einem neuen Signal behandelt wird. |

## Handelslogik
1. Die konfigurierte Kerzenserie abonnieren und auf abgeschlossene Bars warten.
2. Die letzten zwei abgeschlossenen Kerzen zur Musterbewertung pflegen.
3. Wenn Muster und Ratio-Filter übereinstimmen, das direktionale Signal bestimmen und optional Umkehr anwenden.
4. Bestätigen, dass der Handel erlaubt ist (`IsFormedAndOnlineAndAllowTrading`) und dass die relevante Richtung aktiviert ist.
5. Ordergröße basierend auf dem ausgewählten `OpenMode` berechnen und eine Market-Order mit dem Basis-Strategie-Volumen senden.
6. Die interne Kerzenhistorie aktualisieren, damit die neueste Kerze Teil des nächsten Bewertungszyklus wird.

## Implementierungshinweise
- Die Strategie verwendet `StartProtection()`, um den eingebauten Risikomanager zu aktivieren (ohne vordefinierte Stop- oder Take-Profit-Werte). Zusätzliche Filter können bei Bedarf extern hinzugefügt werden.
- Der Indikatorzustand wird nicht in Sammlungen gespeichert; es werden nur die zwei neuesten Kerzen wie für das Muster erforderlich gehalten.
- Der Algorithmus stützt sich ausschließlich auf abgeschlossene Kerzen und vermeidet Intrabar-Berechnungen gemäß den Best Practices der High-Level-API.
