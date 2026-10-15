# OsMaMaster-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die OsMaMaster-Strategie reproduziert das Verhalten des ursprünglichen **OsMaSter_V0** MetaTrader 4-Experten, indem sie sich auf das MACD-Histogramm (OsMA) verlässt, um Impulsumkehrungen zu erkennen. Die Strategie abonniert eine einzelne Kerzenserie und bewertet den jüngsten OsMA-Wendepunkt, sobald eine Kerze geschlossen wird, was mit der Repository-Richtlinie übereinstimmt, nur an fertigen Balken zu arbeiten.

## Handelslogik
- **Indikatorstapel** – ein `MovingAverageConvergenceDivergence`-Indikator wird für jede fertige Kerze verarbeitet. Die Schnell-, Langsam- und Signalperioden spiegeln die Eingabeparameter MQL wider und sind standardmäßig auf 9/26/5 eingestellt.
- **Angewandter Preis** – der Parameter `AppliedPrice` bildet die klassischen MetaTrader `PRICE_*`-Konstanten ab (0 = Schluss, 1 = Eröffnung, 2 = Hoch, 3 = Tief, 4 = Median, 5 = typisch, 6 = gewichtet). Der ausgewählte Preis wird direkt in den Indikator MACD eingespeist.
- **Signalerkennung** – vier OsMA-Messwerte werden gemäß den bereitgestellten `Shift1`–`Shift4`-Offsets verglichen. Die Standardkonfiguration (0,1,2,3) sucht nach einem lokalen Minimum oder Maximum des Histogramms:
  - Lange Einrichtung: `OsMA[shift4] > OsMA[shift3]`, `OsMA[shift3] < OsMA[shift2]`, `OsMA[shift2] < OsMA[shift1]`.
  - Kurzer Aufbau: `OsMA[shift4] < OsMA[shift3]`, `OsMA[shift3] > OsMA[shift2]`, `OsMA[shift2] > OsMA[shift1]`.
- **Einzelpositionsrichtlinie** – ein neuer Trade wird nur übermittelt, wenn derzeit keine Position offen ist, die mit der ursprünglichen Position EA übereinstimmt, die über `ExistPositions` nach vorhandenen Aufträgen gesucht hat.

## Positionsmanagement
- **Stop-Loss** – `StopLossPips` definiert den optionalen Abstand (in Pips) zwischen dem Füllpreis und dem Schutzstopp. Ein Wert von `0` deaktiviert den Stopp.
- **Take-Profit** – `TakeProfitPips` spiegelt den Take-Profit-Parameter von EA wider. Bei der Einstellung `0` wird kein festes Ziel verwendet.
- **Ausführungsmodell** – sowohl Stop als auch Ziel werden anhand der Candle-Extreme (`HighPrice`/`LowPrice`) bewertet. Wenn innerhalb einer Kerze ein Schwellenwert überschritten wird, wird die Position zum Kerzenschluss mithilfe von Marktaufträgen geschlossen.
- **Status-Reset** – immer wenn die Position geschlossen wird, werden alle ausstehenden Stopp-/Zielreferenzen gelöscht, damit der nächste Eintrag sie neu konfigurieren kann.

## Parameter
| Parameter | Beschreibung | Standard |
|-----------|-------------|---------|
| `CandleType` | Zeitrahmen der für alle Berechnungen verwendeten Kerzenserie. | 1 Stunde |
| `FastEmaPeriod` | Schnelle Länge von EMA innerhalb des Indikators MACD. | 9 |
| `SlowEmaPeriod` | Langsame Länge von EMA innerhalb des Indikators MACD. | 26 |
| `SignalPeriod` | Signallänge EMA, das zum Erstellen des Histogramms verwendet wird. | 5 |
| `AppliedPrice` | MetaTrader `PRICE_*`-Code, der definiert, welcher Kerzenpreis den MACD speist. | 0 (schließen) |
| `Shift1` | Erste OsMA-Verschiebung (normalerweise der aktuelle Balken). | 0 |
| `Shift2` | Zweite OsMA-Schicht. | 1 |
| `Shift3` | Dritte OsMA-Schicht. | 2 |
| `Shift4` | Vierte OsMA-Schicht. | 3 |
| `StopLossPips` | Schutzstoppabstand in Pips. | 50 |
| `TakeProfitPips` | Gewinnzielentfernung in Pips. | 50 |

## Konvertierungshinweise
- Die StockSharp-Implementierung behält einen kompakten Ringpuffer der aktuellen OsMA-Werte bei, anstatt wiederholt den Indikatorverlauf anzufordern, und stellt so die Einhaltung der Repository-Regel zur Vermeidung benutzerdefinierter Datensammlungen sicher.
- Bei allen Handelsentscheidungen werden fertige Kerzen verwendet, um zu vermeiden, dass mit unvollständigen Indikatorwerten gearbeitet wird.
- Stop-Loss- und Take-Profit-Logik emulieren die Auftragserteilung von MQL, indem sie Kerzenhochs und -tiefs überwachen und Positionen mit Marktaufträgen schließen.
- Das Standardvolumen der Strategie ist auf **0,01** festgelegt und spiegelt die Standardlosgröße von EA wider.

## Nutzungstipps
- Passen Sie die Perioden `CandleType` und MACD an die Volatilität des Instruments an. Schnellere Märkte können von kürzeren EMA-Längen profitieren.
- Erwägen Sie die Deaktivierung des Take-Profits, indem Sie `TakeProfitPips` auf `0` setzen, wenn Sie erweiterte Trends nutzen und Exits manuell verwalten möchten.
- Stellen Sie beim Experimentieren mit verschiedenen `Shift`-Werten sicher, dass die größte Verschiebung nicht übermäßig groß ist. Die Strategie behält nur so viele Histogrammwerte bei, wie für die maximale Verschiebung erforderlich sind.
- Da Exits anhand von Kerzendaten bewertet werden, verringert die Verwendung kürzerer Zeitrahmen die Verzögerung zwischen der tatsächlichen Schwellenwertüberschreitung und der Exit-Ausführung.
