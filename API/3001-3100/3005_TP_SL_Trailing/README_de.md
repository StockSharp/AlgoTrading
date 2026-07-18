# TP SL Trailing-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine direkte Konvertierung des MetaTrader 5 Expert Advisors "TP SL Trailing". Die Strategie generiert selbst keine Einstiege. Stattdessen verwaltet sie bestehende Positionen, indem sie einen Schutz-Stop-Loss und Take-Profit installiert und den Stop nachzieht, sobald der Trade profitabel wird. Die pip-basierte Konfiguration entspricht den Parametern des Originalskripts und ermöglicht es, die Logik an jedes von StockSharp unterstützte Symbol anzuhängen.

## Handelslogik
- Wenn eine neue Position erscheint, kann die Strategie optional einen anfänglichen Stop-Loss und Take-Profit mit den konfigurierten Pip-Abständen setzen. Dieses Verhalten wird durch das Flag **Only Zero Values** gesteuert, genau wie im ursprünglichen Expert Advisor.
- Für Long-Positionen bewegt die Strategie den Stop-Loss aufwärts, sobald der unrealisierte Gewinn die Summe aus Trailing Stop und Trailing Step überschreitet. Der Stop wird auf `aktueller Preis - Trailing Stop` gesetzt, was garantiert, dass ein Mindestanteil des Gewinns gesichert ist.
- Für Short-Positionen spiegelt die Strategie dieselbe Idee und bewegt den Stop abwärts, sobald der Gewinn die Trailing-Schwellen überschreitet.
- Wenn sowohl Trailing Stop als auch Trailing Step null sind, lässt die Strategie den Stop-Loss unberührt.
- Das Take-Profit-Level wird nie nachgezogen. Es wird nur während der anfänglichen Platzierungsphase gesetzt, wenn **Only Zero Values** aktiviert ist, was das MQL-Verhalten vollständig repliziert.

## Parameter
| Name | Beschreibung |
| --- | --- |
| `CandleType` | Zeitrahmen der Kerzen, die zur Verfolgung von Preisbewegungen verwendet werden. Ein schnellerer Zeitrahmen verbessert die Trailing-Genauigkeit. |
| `StopLossPips` | Abstand in Pips zwischen Eintrittspreis und anfänglichem Stop-Loss. Nur angewendet wenn **Only Zero Values** aktiviert ist. |
| `TakeProfitPips` | Abstand in Pips zwischen Eintrittspreis und anfänglichem Take-Profit. Nur angewendet wenn **Only Zero Values** aktiviert ist. |
| `TrailingStopPips` | Kern-Trailing-Abstand in Pips. Definiert, wie weit hinter dem aktuellen Preis der Stop nach Aktivierung bleiben soll. |
| `TrailingStepPips` | Zusätzlicher Pip-Puffer, der überschritten werden muss, bevor der Stop sich wieder bewegt. Verhindert zu häufige Stop-Updates. |
| `OnlyZeroValues` | Entspricht dem ursprünglichen EA-Flag. Wenn aktiviert, werden anfängliche Schutzorders nur für Positionen erstellt, denen derzeit kein Stop-Loss oder Take-Profit zugewiesen ist. |

## Konvertierungshinweise
- Pip-Abstände werden in Preiseinheiten unter Verwendung des `PriceStep` des Wertpapiers umgerechnet. Dies hält die Logik instrument-agnostisch und spiegelt die 3/5-Stellen-Anpassung in der MQL-Version wider.
- Schutzorders werden neu registriert, wenn die Trailing-Logik den Stop-Loss verschiebt. Aktive Orders einer vorherigen Position werden automatisch storniert, wenn die Positionsgröße auf null zurückkehrt.
- Alle Code-Kommentare sind auf Englisch geschrieben, während diese Dokumentation absichtlich detailliert ist, um jede Entscheidung des Portierungsprozesses nachvollziehbar zu machen.
