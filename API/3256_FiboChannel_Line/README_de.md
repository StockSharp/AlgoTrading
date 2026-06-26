# FiboChannel Line-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **FiboChannel Line-Strategie** ist eine Konvertierung des MetaTrader-Expertenberaters "FIBOCHANNEL". Der ursprüngliche Roboter verließ sich auf die Richtung eines manuell gezeichneten Fibonacci-Kanals, Momentum-Schwingungen auf einem höheren Zeitrahmen und eine Kombination aus linear gewichteten gleitenden Durchschnitten und MACD-Signalen. Der StockSharp-Port behält denselben Geist bei, indem er High-Level-Indikator-Bindungen und integriertes Risikomanagement nutzt.

Wichtige Ideen:

- Der dominanten Tendenz mit einem Paar linear gewichteter gleitender Durchschnitte (LWMA) folgen.
- Momentum-Spitzen um das neutrale Niveau des Momentum-Oszillators bestätigen.
- Trades mit der MACD-Linie-zu-Signallinie-Beziehung filtern.
- Die Steigung eines linearen Regressionskanals statt Diagrammobjekte lesen.
- Positionen über automatischen prozentualen Schutz verwalten.

Die Strategie funktioniert auf jedem Instrument, das Kerzenaggregation unterstützt. Der Standard-Zeitrahmen sind 30-Minuten-Kerzen, die eine Balance zwischen Reaktionsfähigkeit und Indikatorstabilität bieten.

## Handelslogik
1. **Trendfilter** – wenn die schnelle LWMA über der langsamen LWMA schließt, gilt der Markt als bullisch und nur Long-Trades werden bewertet. Wenn sie darunter liegt, werden nur Shorts berücksichtigt.
2. **Momentum-Anforderung** – ein rollierendes Fenster der drei jüngsten Momentum-Werte muss zeigen, dass mindestens ein Wert um den konfigurierten Schwellenwert vom neutralen Niveau 100 abgewichen ist. Dies repliziert die Multi-Bar-Momentum-Stärkeprüfungen der MQL-Version.
3. **MACD-Filter** – Longs erfordern, dass die MACD-Linie über der Signallinie liegt; Shorts erfordern das Gegenteil.
4. **Kanalrichtung** – die Regressionssteigung muss über `Slope Threshold` hinaus positiv (für Longs) oder negativ (für Shorts) sein. Dies ahmt die Aufwärts-/Abwärtskanalvalidierung des ursprünglichen Experten nach.
5. **Einstiege und Umkehrungen** – wenn alle Bedingungen übereinstimmen und keine bestehende Position in diese Richtung vorhanden ist, storniert die Strategie aktive Orders und sendet eine Market-Order mit Größe `Volume + |Position|`. Dies ermöglicht reibungslose Umkehrungen.
6. **Ausstiege** – wenn die Kanalrichtung oder der MACD-Filter den offenen Trade nicht mehr unterstützt, wird die Position nach dem Stornieren ruhender Orders geschlossen. Zusätzlich werden Schutz-Stop-Loss-, Take-Profit- und maximale Drawdown-Regeln über `StartProtection` konfiguriert.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `Candle Type` | Kerzenaggregation für alle Indikatoren. | 30-Minuten-Zeitrahmen |
| `Fast LWMA` | Länge des schnellen linear gewichteten gleitenden Durchschnitts. | 6 |
| `Slow LWMA` | Länge des langsamen linear gewichteten gleitenden Durchschnitts. | 85 |
| `Momentum Period` | Anzahl der Kerzen für den Momentum-Indikator. | 14 |
| `Momentum Threshold` | Minimale absolute Abweichung von 100 innerhalb des Momentum-Puffers. | 0.3 |
| `Channel Length` | Kerzen für die Berechnung der Regressionssteigung. | 50 |
| `Slope Threshold` | Minimaler absoluter Steigungswert zur Bestätigung der Trendrichtung. | 0.0 |
| `MACD Fast` | Schnelle EMA-Periode in der MACD-Berechnung. | 12 |
| `MACD Slow` | Langsame EMA-Periode in der MACD-Berechnung. | 26 |
| `MACD Signal` | Signallinienperiode des MACD. | 9 |
| `Take Profit %` | Schutz-Take-Profit-Abstand in Prozent. | 2 |
| `Stop Loss %` | Schutz-Stop-Loss-Abstand in Prozent. | 1 |
| `Equity Risk %` | Maximaler Konto-Equity-Drawdown vor dem Schließen aller Positionen. | 3 |

Alle numerischen Parameter bieten Optimierungshinweise, die die typischen Bereiche der MQL-Eingaben widerspiegeln.

## Risikomanagement
`StartProtection` ist konfiguriert für:

- Prozentbasierte Stop-Loss und Take-Profit relativ zum Einstiegspreis.
- Equity-Drawdown-Schutz, der die Strategie flacht, wenn der Verlust den konfigurierten Prozentsatz überschreitet.

Diese Schutzmaßnahmen ersetzen die zahlreichen Gleichgewichts-, Trailing- und Break-even-Routinen des ursprünglichen Experten und bieten ein klareres und sichereres Verhalten in StockSharp.

## Unterschiede zur MetaTrader-Version
- Diagrammobjektlesungen wurden durch einen Regressionssteigungsfilter ersetzt, da StockSharp-Strategien nicht mit manuellen Fibonacci-Kanälen interagieren.
- Statt einer Mischung aus geldbasierter Trailing-Logik verlässt sich die Strategie auf `StartProtection`.
- Der Indikator-Stack bleibt gleich (LWMA, Momentum, MACD), ist aber mit High-Level-Bindungen und ohne direktes Indikatorwert-Polling implementiert.
- Alerts, E-Mails und Push-Benachrichtigungen wurden entfernt, da die StockSharp-Umgebung bereits konsolidiertes Logging bietet.

## Verwendungshinweise
1. Hängen Sie die Strategie an ein Portfolio und ein Wertpapier, konfigurieren Sie die Lot-Größe über die `Volume`-Eigenschaft und passen Sie die Parameter nach Bedarf an.
2. Stellen Sie sicher, dass historische Daten für den ausgewählten Kerzentyp verfügbar sind, damit der Momentum-Puffer und der Steigungsindikator sich korrekt bilden können.
3. Führen Sie zuerst im Paper-Trading aus, um den Momentum-Schwellenwert und die Risikoparameter entsprechend der Volatilität des gehandelten Instruments fein abzustimmen.
