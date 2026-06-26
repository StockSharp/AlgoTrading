# Parabolic SAR EA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Übersicht
Die **Parabolic SAR EA-Strategie** ist die StockSharp High-Level-Konvertierung des MetaTrader-Expertenberaters `Parabolic SAR EA.mq5` aus `MQL/23039`. Das ursprüngliche MQL-Skript reagiert auf Parabolic-SAR-Umkehrungen auf einem konfigurierbaren Zeitrahmen, öffnet Marktpositionen mit festen Stop-Loss- und Take-Profit-Abständen, die in MetaTrader-"Pips" ausgedrückt werden (einschließlich Bruchteil-Pip-Unterstützung). Der C#-Port abonniert Kerzen, bindet den eingebauten `ParabolicSar`-Indikator und reproduziert denselben Bar-für-Bar-Entscheidungsprozess unter Einhaltung der StockSharp-Best-Practices.

## Trading-Logik
1. **Datenvorbereitung**
   - Die Strategie abonniert den vom Benutzer ausgewählten Kerzentyp (standardmäßig 30-Minuten-Kerzen) und bindet einen Parabolic-SAR-Indikator mit anpassbarem Beschleunigungsschritt und Maximalwerten.
   - Der SAR-Wert wird für jede Kerze berechnet und über den High-Level-`Bind`-Callback an die Strategie geliefert.
2. **Signalgenerierung**
   - Kaufsignal: wenn der Parabolic-SAR-Wert der abgeschlossenen Kerze streng unterhalb des Kerzen-Tiefs liegt.
   - Verkaufssignal: wenn der Parabolic-SAR-Wert der abgeschlossenen Kerze streng oberhalb des Kerzen-Hochs liegt.
   - Signale werden nur auf abgeschlossenen Kerzen ausgewertet (`CandleStates.Finished`), um die MQL-Neubalken-Verarbeitung zu entsprechen.
3. **Positionsmanagement**
   - Die entgegengesetzte Exposition wird vor einem neuen Einstieg geflacht, indem die angeforderte Market-Order-Größe um den absoluten aktuellen Positionswert erhöht wird, was die MetaTrader-Sequenz `ClosePosition` plus `OpenPosition` repliziert.
   - Jeder Einstieg berechnet die Schutz-Stop-Loss- und Take-Profit-Levels unter Verwendung derselben Pip-zu-Preis-Konvertierungsregeln wie MetaTrader neu (3/5-stellige Instrumente erhalten einen ×10-Multiplikator für den `PriceStep`).
4. **Schutzausgänge**
   - Bei jeder abgeschlossenen Kerze prüft die Strategie, ob das Hoch/Tief den gespeicherten Stop-Loss- oder Take-Profit-Level verletzt. Wenn ausgelöst, wird die Position mit einer Market-Order geschlossen und die entsprechenden Ziele werden gelöscht.
   - Die Schutzlogik feuert vor neuen Signalen auf demselben Balken und spiegelt damit das ursprüngliche Expertenberater-Verhalten wider, bei dem Stop-Orders broker-seitig liegen.

## Indikator- und Datenhinweise
- Verwendet den eingebauten `ParabolicSar`-Indikator von StockSharp mit den Parametern `SarStep` und `SarMaximum`.
- Das Kerzen-Abonnement wird über `SubscribeCandles` abgewickelt, ohne den Indikator zu `Strategy.Indicators` hinzuzufügen, wie von den Projektrichtlinien gefordert.
- Das Handeln ist nur erlaubt, wenn `IsFormedAndOnlineAndAllowTrading()` true meldet, um sicherzustellen, dass Live-Daten vorhanden sind und der Connector die Ordereinreichung erlaubt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `TradeVolume` | `1` | Market-Order-Größe in Lots. Eine Aktualisierung des Werts aktualisiert auch `Strategy.Volume`. |
| `StopLossPips` | `50` | Stop-Loss-Abstand in MetaTrader-Pips. Ein Pip entspricht `PriceStep × 10` bei Instrumenten mit 3 oder 5 Dezimalstellen, sonst nur `PriceStep`. Auf `0` setzen zum Deaktivieren. |
| `TakeProfitPips` | `50` | Take-Profit-Abstand in MetaTrader-Pips unter Verwendung derselben Konvertierungsregeln wie der Stop-Loss. Auf `0` setzen zum Deaktivieren. |
| `SarStep` | `0.02` | Beschleunigungsschritt des Parabolic-SAR-Indikators. |
| `SarMaximum` | `0.2` | Maximaler Beschleunigungsfaktor für Parabolic SAR. |
| `CandleType` | `30m timeframe` | Kerzentyp für Berechnungen. Unterstützt jeden `DataType` aus `TimeFrame`. |

## Risikomanagement und Verhalten
- Stop-Loss und Take-Profit werden nach jeder Ausführung neu berechnet und intern gespeichert; keine ausstehenden Orders werden an der Börse registriert.
- Wenn beide Schutzlevel innerhalb einer einzigen Kerze ausgelöst werden, feuert die Stop-Loss-Prüfung zuerst und repliziert damit die konservative Behandlung der Quell-MQL-Logik.
- Wenn der Connector keinen gültigen `PriceStep` meldet, fällt die Konvertierung auf `0.0001` zurück, um Schutzlevel mit Nullabstand zu vermeiden.
- Es wird kein Averaging oder Pyramidisieren durchgeführt; die Strategie operiert mit einer einzigen Nettoposition und dreht die Richtung um, wenn der Parabolic SAR den Preis kreuzt.

## Konvertierungshinweise
- MetaTrader `InpBarCurrent` entspricht 1, was bedeutet, dass der EA die vorherige abgeschlossene Kerze auswertet. Der StockSharp-Port erzielt dasselbe Ergebnis, indem im `Bind`-Callback nur `Finished`-Kerzen verarbeitet werden.
- Der ursprüngliche Expertenberater verwendete `CheckVolumeValue` zur Validierung von Lots und Broker-Beschränkungen. StockSharp delegiert diese Prüfungen an den Connector, während der `TradeVolume`-Parameter weiterhin eine positive Volumenanforderung durchsetzt.
- Die Python-Implementierung ist absichtlich weggelassen, entsprechend den Aufgabenanforderungen.
