# E-News Lucky Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die **E-News Lucky Strategie** ist ein StockSharp-Port des MetaTrader Expert Advisors `e-News-Lucky`. Das System automatisiert den klassischen News-Breakout-Ansatz:

- Zu einer konfigurierbaren `PlacementTime` sendet es sowohl Buy-Stop- als auch Sell-Stop-Orders um den aktuellen Preis, versetzt um `DistancePips`.
- Wenn eine der ausstehenden Orders ausgeführt wird, wird die entgegengesetzte Order sofort storniert. Initiale Schutz-Stop-Loss- und Take-Profit-Niveaus werden gemäß den konfigurierten Pip-Offsets angehängt.
- Ein Trailing Stop kann über `TrailingStopPips` und `TrailingStepPips` aktiviert werden, um Gewinne zu sichern, wenn sich der Trade in die günstige Richtung bewegt.
- Zur konfigurierten `CancelTime` werden alle verbleibenden ausstehenden Orders entfernt und offene Positionen geschlossen, um Risiken außerhalb des Handelsfensters zu vermeiden.

Die Strategie verwendet Kerzendaten (`CandleType`, standardmäßig 1 Minute) nur zur Verfolgung der geplanten Zeiten und zur Aktualisierung des Trailing Stops. Sie stützt sich nicht auf Indikatorberechnungen.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `Volume` | Ordervolumen für jeden ausstehenden Einstieg. Die Strategie sendet symmetrische Buy-Stop- und Sell-Stop-Orders mit diesem Volumen. |
| `StopLossPips` | Abstand zwischen dem Einstiegspreis und dem Schutz-Stop-Loss, ausgedrückt in Pips. Auf null setzen zum Deaktivieren des Stops. |
| `TakeProfitPips` | Abstand zwischen dem Einstiegspreis und dem Gewinnziel in Pips. Auf null setzen zum Deaktivieren des Ziels. |
| `TrailingStopPips` | Trailing-Stop-Distanz in Pips. Die Trailing-Engine wird nur aktiv, wenn dieser Wert größer als null ist. |
| `TrailingStepPips` | Minimaler Pip-Gewinn erforderlich, bevor der Trailing Stop wieder verschoben wird. Verhindert übermäßige Stop-Aktualisierungen in seitwärts tendierenden Märkten. |
| `DistancePips` | Versatz (in Pips) vom aktuellen Preis zur Platzierung der Stop-Orders. |
| `PlacementTime` | Tageszeit (Broker-/Server-Zeit), zu der die ausstehenden Orders platziert werden. Standard: 10:30. |
| `CancelTime` | Tageszeit, zu der ausstehende Orders storniert und offene Positionen geschlossen werden. Standard: 22:30. |
| `CandleType` | Kerzenserie für Terminierung und Trailing. Standard: 1-Minuten-Zeitrahmen. |

## Implementierungshinweise
- Die Pip-Größe folgt der MetaTrader-Logik: Wenn das Symbol 3 oder 5 Stellen hat, multipliziert die Strategie den Preisschritt mit 10, um in Pip-Einheiten zu arbeiten.
- Alle Preise werden auf den Instrument-Preisschritt normalisiert, bevor Orders gesendet werden.
- Trailing Stops vergleichen den letzten Schlusskurs mit `PositionPrice` und verschieben den Schutz-Stop nur, wenn der Gewinn sowohl `TrailingStopPips` als auch `TrailingStepPips` übersteigt.
- Ausstehende Orders werden jeden Handelstag neu erstellt, wenn die Platzierungszeit erreicht wird. Überprüfungen der Stornierungszeit stellen sicher, dass alle Positionen am Ende des Fensters flach sind.

## Verwendungstipps
1. Hängen Sie die Strategie an ein liquides Instrument mit engen Spreads; die Breakout-Abstände setzen nachrichtenartiges Preisverhalten voraus.
2. Legen Sie `PlacementTime` und `CancelTime` gemäß dem relevanten Wirtschaftskalender fest.
3. Passen Sie die Pip-Abstände an die Instrumentvolatilität an. Größere Werte reduzieren die Wahrscheinlichkeit von falschen Signalen, während kleinere Werte frühere Bewegungen erfassen können, aber das Whipsaw-Risiko erhöhen.
4. Deaktivieren Sie Trailing, indem Sie `TrailingStopPips` bei null lassen, wenn feste Stops bevorzugt werden.
5. Überwachen Sie Slippage und Spread bei high-impact-Nachrichten, um sicherzustellen, dass ausstehende Orders wie erwartet gefüllt werden.
