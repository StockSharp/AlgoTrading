# Schnelle langsame RVI-Crossover-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert den MetaTrader-Expertenberater `_HPCS_FastSlowRVIsCrossOver_MT4_EA_V01_WE`. Es wird gehandelt, wenn die Hauptlinie des Relative Vigor Index (RVI) während der konfigurierten Handelssitzung seine Signallinie kreuzt. Pro Kerze ist nur ein Trade zulässig, und die Strategie unterstützt optionale Stop-Loss-, Take-Profit- und Trailing-Stop-Distanzen, ausgedrückt in Pips.

## Handelslogik
1. Erstellen Sie zeitbasierte Standardkerzen, die durch den Parameter **Kerzentyp** ausgewählt werden.
2. Berechnen Sie den RVI mit der konfigurierten **RVI-Periode** und einem einfachen gleitenden Durchschnitt über 4 Perioden als Signallinie.
3. Wenn der RVI über die Signallinie steigt, schließen Sie alle Short-Positionen und eröffnen/skalieren Sie in eine Long-Position.
4. Wenn der RVI unter die Signallinie fällt, schließen Sie alle Long-Positionen und eröffnen/skalieren Sie in eine Short-Position.
5. Ignorieren Sie Signale, die außerhalb des Intervalls **Startzeit** und **Stoppzeit** auftreten.
6. Erteilen Sie Schutzanordnungen gemäß den ausgewählten Risikoparametern. Trailing Stops werden von der Schutz-Engine StockSharp verwaltet.
7. Vermeiden Sie doppelte Einträge bei derselben Kerze, indem Sie nur einmal pro Balken reagieren.

## Parameter
| Name | Beschreibung |
|------|-------------|
| **RVI-Zeitraum** | Anzahl der vom Relative Vigor Index verwendeten Balken. |
| **Gewinnmitnahme (Pips)** | Optionale Take-Profit-Distanz, gemessen in Pips. Zum Deaktivieren auf Null setzen. |
| **Stop-Loss (Pips)** | Optionale Stop-Loss-Distanz, gemessen in Pips. Zum Deaktivieren auf Null setzen. |
| **Trailing Stop (Pips)** | Optionale Trailing-Stop-Distanz in Pips. Auf Null setzen, um das Nachziehen zu deaktivieren. |
| **Trailing Step (Pips)** | Minimale günstige Bewegung, die erforderlich ist, bevor der Trailing Stop verschärft wird. Funktioniert nur, wenn der Trailing Stop aktiv ist. |
| **Volumen** | Bei jedem Eintrag übermitteltes Bestellvolumen. |
| **Kerzentyp** | Zeitrahmen oder benutzerdefinierter Kerzendatentyp, der für die Analyse verwendet wird. |
| **Startzeit** | Beginn des täglichen Handelsfensters (einschließlich). |
| **Stoppzeit** | Ende des täglichen Handelsfensters (exklusiv). |

## Notizen
- Die Pip-Größe wird an die Sicherheits-Tick-Größe angepasst, um der Handhabung von MetaTrader-Punkten zu entsprechen (5- und 3-stellige Symbole verwenden einen 10-fachen Multiplikator).
- Rufen Sie `StartProtection` einmal innerhalb von `OnStarted` auf, um Schutzanordnungen und die Nachverfolgungsverwaltung zu aktivieren.
- Alle Kommentare im Quellcode sind in englischer Sprache verfasst, wie es die Projektrichtlinien vorschreiben.
