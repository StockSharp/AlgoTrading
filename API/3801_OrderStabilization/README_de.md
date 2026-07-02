# Strategie zur Auftragsstabilisierung
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **Order Stabilization Strategy** ist eine Umsetzung des MetaTrader-Expertenberaters `hjueiisyx8lp2o379e_www_forex-instruments_info.mq4`. Der ursprüngliche Roboter platziert ein Paar Stop-Orders rund um den aktuellen Preis und wartet auf einen Ausbruch. Sobald eine Position eröffnet wird, überwacht das System die jüngsten Kerzenkörper, um festzustellen, ob die Preisbewegung ins Stocken geraten („stabilisiert“) ist, und verlässt den Handel, wenn der Markt an Dynamik verliert oder eine vordefinierte Gewinnschwelle erreicht wird.

Dieser C#-Port behält die gleiche Logik bei, indem er das übergeordnete StockSharp API verwendet. Es basiert auf abgeschlossenen Kerzen statt auf rohen Ticks, was das Verhalten beim Backtesting und Live-Handel deterministisch macht.

## Handelsregeln
1. Wenn keine offenen Positionen oder aktiven Aufträge vorhanden sind, legt die Strategie einen **Kaufstopp** oberhalb des Marktes und einen **Verkaufsstopp** unterhalb des Marktes fest. Die Entfernung wird in MetaTrader Punkten gemessen (normalerweise gleich einem Pip).
2. Wenn eine Stop-Order ausgeführt wird:
   - Der ausgeführte Auftrag eröffnet eine Position von `OrderVolume` Lots.
   - Die entgegengesetzte Stop-Order bleibt ausstehend, um einen Ausbruch in die andere Richtung abzufangen.
3. Während eine Position offen ist, überprüft die Strategie die Körpergröße der beiden zuletzt abgeschlossenen Kerzen:
   - Wenn der letzte Kerzenkörper kleiner als `StabilizationPoints` ist und der variable Gewinn höher als `ProfitThreshold` ist, wird die Position geschlossen und die entgegengesetzte ausstehende Order storniert.
   - Wenn zwei aufeinanderfolgende Kerzen kleiner als `StabilizationPoints` sind, wird der Handel unabhängig vom aktuellen Gewinn geschlossen.
   - Wenn der Gewinn `AbsoluteFixation` erreicht, wird der Handel sofort geschlossen.
4. Ausstehende Bestellungen werden nach `ExpirationMinutes` storniert und neu erstellt, es sei denn, der Wert ist auf Null gesetzt (unendliche Lebensdauer).

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `OrderVolume` | Handelsvolumen in Lots, das für beide Stop-Eingaben verwendet wird. | `0.1` |
| `OrderDistancePoints` | Abstand zwischen dem aktuellen Schlusskurs und jeder Stop-Order, ausgedrückt in MetaTrader Punkten. | `20` |
| `ProfitThreshold` | Mindesterforderlicher variabler Gewinn (Kontowährung), bevor ein durch Stabilisierung ausgelöster Ausstieg zulässig ist. | `-2` |
| `AbsoluteFixation` | Gewinnniveau (Kontowährung), das einen sofortigen Ausstieg erzwingt. | `30` |
| `StabilizationPoints` | Maximale Kerzenkörpergröße (Punkte), die einen flachen Markt signalisiert. | `25` |
| `ExpirationMinutes` | Lebensdauer ausstehender Stop-Orders in Minuten. `0` deaktiviert den Ablauf. | `20` |
| `CandleType` | Kerzentyp, der zur Bewertung der Stabilisierung verwendet wird (standardmäßig 5-Minuten-Zeitrahmen). | `TimeFrame(5m)` |

## Konvertierungshinweise
- Der ursprüngliche Fachberater operierte mit Chart-Ticks. Dieser Port wertet nur fertige Kerzen aus, wobei die Logik erhalten bleibt und gleichzeitig reproduzierbare Backtests gewährleistet werden.
- MetaTrader „Punkte“ werden dem StockSharp `PriceStep` zugeordnet. Fehlt dem Instrument eine Preisstufe, wird eine Stufe von `1` angenommen.
- Der Gewinn wird mithilfe von `PriceStep` und `StepPrice` angenähert, um Preisbewegungen in die Kontowährung umzurechnen.
- Alle Codekommentare wurden in Englisch umgeschrieben und die Parametermetadaten enthalten benutzerfreundliche Beschreibungen mit Gruppierung.

## Nutzung
1. Fügen Sie die Strategie zu Ihrer StockSharp-Lösung hinzu und weisen Sie das gewünschte Wertpapier und Portfolio zu.
2. Konfigurieren Sie die Parameter, insbesondere den Zeitrahmen der Kerze und die Entfernung in Punkten, um sie an die Eigenschaften des Instruments anzupassen.
3. Starten Sie die Strategie. Es wird gepaarte Stop-Orders einreichen und Positionen gemäß der oben beschriebenen Stabilisierungslogik verwalten.

## Weitere Ideen
- Experimentieren Sie mit verschiedenen Kerzenintervallen, um Reaktionsfähigkeit und Geräuschfilterung in Einklang zu bringen.
- Kombinieren Sie die Strategie mit Volatilitätsfiltern (ATR, Bollinger-Bänder), um den Handel während extrem ruhiger Sitzungen zu vermeiden.
- Erweitern Sie die Logik mit Trailing Stops oder teilweisen Positionsausstiegen, sobald Sie sich dem absoluten Gewinnziel nähern.
