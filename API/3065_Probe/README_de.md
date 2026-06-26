# Probe Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Probe-Strategie reproduziert den MetaTrader 5 Expert Advisor "Probe" innerhalb des StockSharp High-Level-Frameworks. Sie überwacht den Commodity Channel Index (CCI) auf einem konfigurierbaren Zeitrahmen und reagiert, wenn der Oszillator aus einem symmetrischen Kanal ausbricht. Bei einem Ausbruch platziert die Strategie eine Stop-Order mit einem pip-basierten Abstand vom aktuellen Marktpreis. Der Ansatz zielt darauf ab, die Momentum-Fortsetzung nach dem Ausbruch zu erfassen, während das Risiko durch pip-basierte Schutzniveaus und einen adaptiven Trailing Stop begrenzt wird.

## Handelslogik
1. Den CCI auf dem konfigurierten Kerzentyp berechnen.
2. Die vorherigen und aktuellen CCI-Werte verfolgen, um zu erkennen, wann der Indikator die untere oder obere Kanalgrenze verlässt.
3. Wenn der CCI aufwärts durch `-CCI Channel` kreuzt, eine Kauf-Stop-Order oberhalb des letzten Schlusskurses mit dem `Indent (pips)`-Abstand einreichen.
4. Wenn der CCI abwärts durch `+CCI Channel` kreuzt, eine Verkauf-Stop-Order unterhalb des letzten Schlusskurses mit demselben Pip-Indent einreichen.
5. Es kann immer nur eine ausstehende Stop-Order aktiv bleiben. Entgegengesetzte Orders werden storniert und neue Signale werden ignoriert, während eine Order aktiv ist.

## Order-Management
- Ausstehende Stop-Orders werden zurückgezogen, wenn sich der Markt mehr als `1.5 * Indent (pips)` vom Einstiegspreis entfernt. Dies spiegelt die MetaTrader-Logik wider, die verhindert, dass veraltete Orders im Buch verbleiben, wenn das Momentum nachlässt.
- Sobald eine Stop-Order ausgeführt ist, speichert die Strategie den ausgeführten Preis als Eintragsreferenz. Alle entgegengesetzten ausstehenden Orders werden sofort storniert.

## Risikomanagement
- Ein initialer Stop-Loss wird aus `Stop Loss (pips)` abgeleitet und über internes Monitoring an die aktive Position angehängt. Wenn der Preis den Stop berührt, wird die Position mit einer Marktorder ausgestiegen.
- Das Trailing-Verhalten beginnt, nachdem der schwebende Gewinn `Trailing Stop (pips) + Trailing Step (pips)` überschreitet. Der Stop wird dann bewegt, um Gewinne zu sichern, während die minimale Trailing-Distanz eingehalten wird.
- Alle pip-basierten Abstände passen sich automatisch für 3- und 5-stellige Kursnotierungen an, indem die Börsen-Tick-Größe skaliert wird.

## Parameter
| Parameter | Beschreibung |
|-----------|--------------|
| `CandleType` | Primärer Zeitrahmen für den Aufbau von Kerzen und die Berechnung des CCI. |
| `CciLength` | Mittelungsperiode des CCI-Oszillators. |
| `CciChannelLevel` | Absoluter CCI-Schwellenwert, der den symmetrischen Ausbruchskanal bildet. |
| `IndentPips` | Pip-Abstand, der dem letzten Schlusskurs beim Platzieren der ausstehenden Stop-Order hinzugefügt wird. |
| `StopLossPips` | Schutz-Stop-Loss-Abstand gemessen in Pips. |
| `TrailingStopPips` | Gewinnschwelle in Pips, die erforderlich ist, bevor der Trailing Stop aktiviert wird. |
| `TrailingStepPips` | Zusätzlicher Gewinnabstand, der benötigt wird, bevor der Trailing Stop wieder bewegt wird. |

## Hinweise
- Verwenden Sie die `Volume`-Eigenschaft der Strategie, um die gehandelte Größe zu steuern.
- Die Strategie ist für Single-Position-Netting ausgelegt und entspricht dem ursprünglichen Expert Advisor-Verhalten.
- Chart-Rendering zeichnet Kerzen, den CCI-Indikator und ausgeführte Trades, wenn ein Chartbereich verfügbar ist.
