# M.A. Pausenstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie repliziert das Verhalten der MetaTrader-Experten „M.A break mt5 buy“ und „M.A break mt5 Sell“, indem beide Breakout-Richtungen in einer einzigen StockSharp-Implementierung kombiniert werden. Es beobachtet eine konfigurierbare Kerzenserie, analysiert mehrere exponentielle gleitende Durchschnitte und bestätigt eine Kerze mit starkem Impuls, bevor Geschäfte eröffnet werden. Die Positionen werden durch feste Schutzstopps und in Pips gemessene Ziele verwaltet.

## Handelslogik

1. **Trendbestätigung.** Zwei EMA-Paare (schnell vs. langsam) müssen in der Handelsrichtung der fertigen Kerze ausgerichtet sein. Bei Long-Positionen müssen beide schnellen Durchschnittswerte über ihren langsamen Gegenstücken liegen; Bei Kurzfilmen sind die Verhältnisse umgekehrt. Die vorherige offene Kerze muss sich außerdem auf der richtigen Seite eines speziellen EMA-Filters befinden.
2. **Messung des Ruhebereichs.** Eine konfigurierbare Anzahl früherer Kerzen (mit Ausnahme der letzten Impulskerze) definiert die „Ruhe“-Periode. Ihr höchster Bereich wird mit einem minimalen Pip-Schwellenwert verglichen.
3. **Impulserkennung.** Die letzte fertige Kerze muss sich mindestens um das `ImpulseStrength`-fache des Ruhebereichs ausdehnen. Kerzengrößenbeschränkungen in Pips können erzwungen werden, um ungewöhnlich kleine oder große Bewegungen zu ignorieren.
4. **Kerzenvorlage.** Die Impulskerze muss eine bestimmte Dochtstruktur aufweisen:
   - Long-Trades: bullischer Körper, der obere Docht überschreitet nicht `BullUpperWickPercent` der Kerzenspanne und der untere Docht mindestens `BullLowerWickPercent` der Spanne.
   - Short-Trades: bärischer Körper, oberer Docht mindestens `BearUpperWickPercent` und unterer Docht nicht größer als `BearLowerWickPercent` der Spanne.
5. **Pullback-Bedingung.** Das Impulstief (für Longs) oder das Hoch (für Shorts) muss einen zusätzlichen EMA testen, um sicherzustellen, dass der Ausbruch aus einem Pullback hervorgegangen ist.
6. **Positionskontrolle.** Es ist nur eine Nettoposition zulässig. Die Strategie schließt die Gegenseite vor dem Eingehen eines neuen Handels und eröffnet niemals eine Position gegen den Trendfilter.
7. **Exit-Management.** Stop-Loss- und Take-Profit-Level werden in Pips vom Einstiegspreis berechnet. Bei jeder fertigen Kerze wird geprüft, ob der Preis die Schutzniveaus berührt hat, und der Kurs wird entsprechend beendet.

## Parameter

| Parameter | Beschreibung |
|-----------|-------------|
| **Kerzentyp** | Für alle Berechnungen verwendete primäre Kerzenserie. |
| **Schneller MA 1 / Langsamer MA 1** | Perioden des ersten EMA-Paares, das den primären Trend definiert. |
| **Schneller MA 2 / Langsamer MA 2** | Perioden des sekundären EMA-Paares, die als zusätzlicher Trendfilter verwendet werden. |
| **Filter MA öffnen** | EMA Zeitraum, der den vorherigen Kerzeneröffnungspreis filtert. |
| **Rückzug MA** | EMA Zeitraum, dessen Wert vom Impulsdocht berührt werden muss. |
| **Ruhige Bars** | Anzahl der historischen Kerzen, die zur Messung der ruhigen Marktspanne verwendet werden. |
| **Ruhebereich (Pips)** | Es ist ein minimaler Pip-Bereich über die ruhigen Kerzen hinweg erforderlich, bevor ein Ausbruch in Betracht gezogen wird. |
| **Impulsmultiplikator** | Minimales Verhältnis zwischen Impulskerzengröße und Ruhebereich. |
| **Min./Max. Kerzengröße (Pips)** | Optionale Grenzen für den Impulskerzenbereich. Null deaktiviert die entsprechende Grenze. |
| **Oberer Bull-Docht % / Unterer Bull-Docht %** | Formfilter für die bullische Impulskerze, ausgedrückt als Prozentsätze der Kerzenspanne. |
| **Bär oberer Docht % / Bär unterer Docht %** | Formfilter für die bärische Impulskerze. |
| **Volumen** | Ordergröße in Lots, die sowohl für Long- als auch für Short-Einträge verwendet wird. |
| **Stop-Loss (Pips)** | Abstand zum Schutzstopp, gemessen vom Einstiegspreis. Null deaktiviert den Stopp. |
| **Take-Profit (Pips)** | Abstand zum Gewinnziel. Null deaktiviert das Ziel. |
| **Long aktivieren / Short aktivieren** | Schalten Sie den Breakout-Handel in jede Richtung unabhängig um. |

## Nutzungshinweise

- Konfigurieren Sie die Kerzenserie so, dass sie dem vom ursprünglichen Experten verwendeten Zeitrahmen entspricht (z. B. M5 oder H1). Der Standardwert ist ein Zeitrahmen von 5 Minuten.
- Die Strategie speichert nur den aktuellen Verlauf, der für die Berechnung des Ruhebereichs erforderlich ist, und verhindert so unnötigen Speicherverbrauch.
- Die Einstiegspreise werden durch den Impulskerzenschluss angenähert, der dem ursprünglichen MetaTrader-Verhalten der Platzierung von Marktaufträgen zu Beginn des nächsten Balkens entspricht.
- Stop-Loss- und Take-Profit-Level werden bei jeder abgeschlossenen Kerze bewertet. Wenn beide Ebenen innerhalb desselben Balkens erreicht werden, hat der Stopp Vorrang, was die konservative Handhabung widerspiegelt, die in den Quellexperten verwendet wird.
- Wenn Sie nur eine Richtung aktivieren, werden die ursprünglichen Expertenberater „Kauf“ oder „Verkauf“ reproduziert, während beide Schalter aktiv bleiben, um einen symmetrischen Breakout-Handel zu ermöglichen.

## Konvertierungsdetails

- Beide ursprünglichen MQ5-Dateien wurden in UTF-16 codiert und aus Blöcken erstellt, die von der FXD-Engine generiert wurden. Jeder Block wurde in explizite C#-Logik übersetzt.
- EMA-Vergleiche und Candlestick-Vorlagen folgen den gleichen Verschiebungen wie die MetaTrader-Version (`Shift = 1`), was bedeutet, dass die Strategie immer vollständig geschlossene Kerzen bewertet.
- Auf die virtuelle Stopplogik und Diagrammbeschriftungen aus den MQ5-Skripten wurde bewusst verzichtet, da sie keinen Einfluss auf die Auftragserteilung haben.

## Testen

Kompilieren Sie die Lösung über `AlgoTrading.sln` oder führen Sie die Strategie im Strategietester StockSharp aus. Passen Sie die Preisstufe des Instruments an, wenn diese Informationen in den Sicherheitsmetadaten fehlen. Die Implementierung greift auf `0.0001` zurück, um gängige FX-Pip-Werte zu emulieren.
