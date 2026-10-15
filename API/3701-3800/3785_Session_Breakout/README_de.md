# Sitzungs-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Session Breakout-Strategie repliziert den Expertenberater „Session Breakout“ von MetaTrader. Es beobachtet die europäische Morgensitzung
auf und misst seine Preisspanne. Wenn diese Spanne ausreichend eng ist, bereitet sich die Strategie auf den Handel mit Ausbrüchen während des US-Zeitraums vor.
Nachmittagssitzung mit StockSharps High-Level-API. Die Implementierung erzwingt höchstens einen Long- und einen Short-Eintrag pro Tag a
Und fügt jeder Position automatisch Schutzaufträge (Stop-Loss und Take-Profit) hinzu.

## Handelslogik
- Setzen Sie den Status zu Beginn jedes Handelstages zurück und überspringen Sie Wochenenden. Montags sind optional und werden durch einen Parameter gesteuert.
- Verfolgen Sie fertige Kerzen während der europäischen Sitzung (Standard 06:00–12:00 Uhr) und zeichnen Sie das höchste Hoch und das niedrigste Tief auf.
- Zu Beginn der US-Sitzung wird der erfasste Bereich als „klein“ klassifiziert, wenn seine Breite kleiner als „SmallSessionThreshol“ ist
dPips`.
- Wenn die Spanne klein ist, beobachten Sie die US-Sitzungskerzen (Standard 12:00–16:00) und warten Sie, bis mindestens ein US-Balken geschlossen hat („Eu
RopeSessionStartHour + 5` to `EuropeSessionStartHour + 10`).
- Ein langer Ausbruch wird ausgelöst, wenn die gesamte Kerze über dem europäischen Hoch zuzüglich eines konfigurierbaren Puffers („BreakoutBuffer“) bleibt
Pips`). Für einen kurzen Ausbruch muss die Kerze unter dem europäischen Tief abzüglich des Puffers bleiben.
- Nach dem Eingehen einer Position fügt die Strategie Stop-Loss- und Take-Profit-Levels hinzu, die in Pips ausgedrückt werden, und verhindert ein zusätzliches En
versucht den Rest des Tages in die gleiche Richtung.

## Parameter
| Parameter | Beschreibung |
|-----------|-------------|
| `Volume` | Das Ordervolumen wird sowohl für Long- als auch für Short-Breakouts verwendet. |
| `EuropeSessionStartHour` | Stunde, in der die europäische Reichweitenverfolgung beginnt. |
| `EuropeSessionEndHour` | Stunde, in der die europäische Reichweitenverfolgung stoppt. |
| `UsSessionStartHour` | Stunde, die den Beginn des US-Sitzungsfensters markiert. |
| `UsSessionEndHour` | Stunde, die das Ende des US-Sitzungsfensters markiert. |
| `SmallSessionThresholdPips` | Maximale Breite (in Pips), damit der europäische Bereich als Squeeze gilt. |
| `BreakoutBufferPips` | Zusätzlicher Puffer oberhalb/unterhalb der Spanne hinzugefügt, bevor Ausbrüche ausgelöst werden. |
| `TradeOnMonday` | Ermöglicht den Handel montags. Wochenenden werden immer ausgelassen. |
| `TakeProfitPips` | Abstand zwischen Einstiegspreis und Take-Profit-Niveau. |
| `StopLossPips` | Abstand zwischen dem Einstiegspreis und dem Stop-Loss-Level. |
| `CandleType` | Für alle Berechnungen verwendete Kerzenserie (standardmäßig 15-Minuten-Kerzen). |

## Notizen
- Die Pip-Größe wird vom Instrument `PriceStep` abgeleitet. Passen Sie die Pip-basierten Parameter an die Vertragsspezifikation an
s des ausgewählten Wertpapiers.
- Da Aufträge generiert werden, wenn eine qualifizierte Kerze schließt, erfolgt die Ausführung bei Backtests zum Schlusskurs dieser Kerze. Liv
Die Füllungen können je nach Marktbedingungen variieren.
- Pro Tag kann nur ein Long- und ein Short-Trade eröffnet werden. Die Logik spiegelt das ursprüngliche Verhalten des Expert Advisors bei der Verwendung von S wider
tockSharps positionsbasierte Risikomanagement-Helfer.
