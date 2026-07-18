# Halblange WWW-Forex-Instrumente-Info-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie repliziert das Verhalten des Experten „Semilong“ MetaTrader. Es überwacht den Abstand zwischen dem aktuellen Geldkurs und zwei historischen Schlusskursen, die durch konfigurierbare Verschiebungen getrennt sind. Wenn der aktuelle Markt weit genug unter (oder über) dem älteren Schlusskurs notiert, während sich der ältere Schlusskurs gleichzeitig von einer noch älteren Referenz entfernt hat, eröffnet die Strategie eine Long- (oder Short-)Position. Das Positionsmanagement spiegelt das ursprüngliche Skript mit konfigurierbarem Take-Profit, Stop-Loss, optionalem Trailing-Stop und einem Auto-Lot-Modul wider, das die Größe nach aufeinanderfolgenden Verlusten reduziert.

## Signalerzeugung
- **Historische Verschiebungen** – `ShiftOne` wählt aus, aus wie vielen fertigen Kerzen der erste Vergleichsschluss entnommen wird, während `ShiftTwo` einen zusätzlichen Offset für den zweiten Schluss hinzufügt.
- **Abweichungsfilter** – `MoveOnePoints` definiert, wie weit das aktuelle Gebot vom ersten verschobenen Schlusskurs abweichen muss, und `MoveTwoPoints` misst den Abstand zwischen beiden verschobenen Schlusskursen.
- **Lange Einrichtung** – Wird ausgelöst, wenn das aktuelle Gebot mindestens `MoveOnePoints` unter dem ersten verschobenen Schlusskurs liegt und der erste verschobene Schlusskurs mindestens `MoveTwoPoints` über dem zweiten verschobenen Schlusskurs liegt.
- **Kurzes Setup** – Wird ausgelöst, wenn das aktuelle Gebot mindestens `MoveOnePoints` über dem ersten verschobenen Schlusskurs liegt und der erste verschobene Schlusskurs mindestens `MoveTwoPoints` unter dem zweiten verschobenen Schlusskurs liegt.
- Die Strategie wartet auf abgeschlossene Kerzen, ignoriert Signale, wenn Aufträge bereits aktiv sind, und erfordert vor dem Handel eine positive freie Marge.

## Handelsmanagement
- **Erste Schutzaufträge** – Anstatt ausstehende Aufträge zu registrieren, emuliert die Strategie das ursprüngliche Verhalten, indem sie den Einstiegspreis verfolgt und den Markt verlässt, sobald die Bewegung Folgendes erreicht:
  - `ProfitPoints` (plus der aktuelle Spread) zugunsten der Position.
  - `LossPoints` gegen die Position.
- **Trailing Stop** – Wenn `TrailingPoints` größer als Null ist, zeichnet die Strategie den besten nach dem Einstieg erreichten Preis auf. Wenn der Preis um die Nachlaufdistanz zurückgeht, wird die Position geschlossen.
- **Einzelpositionsrichtlinie** – Es ist jeweils nur eine Marktposition zulässig; Neue Signale werden ignoriert, während ein Handel läuft oder während Abschlussaufträge ausstehen.

## Positionsgrößen
- **Festes Volumen** – Wenn `UseAutoLot` deaktiviert ist, verwendet jeder Handel `FixedVolume` (angepasst an den Volumenschritt und die Grenzen des Instruments).
- **Automatische Lotberechnung** – Wenn diese Option aktiviert ist, wird die freie Marge durch `AutoMarginDivider * 1000` geteilt und auf das nächste ganze Lot gerundet. Wenn nacheinander mindestens zwei Verlustgeschäfte stattgefunden haben, wird das Volumen proportional um `lossStreak / DecreaseFactor` reduziert, was der MT4-Verringerungslogik nachahmt.
- Das Volumen wird zwischen `FixedVolume` und 99 Lots festgelegt und dann an die Volumenschritt-/Min.-/Max.-Grenzwerte des Instruments angepasst.

## Zusätzliche Hinweise
- Der Spread wird vom aktuell besten Brief-/Gebotskurs abgelesen und zur Vergrößerung des Gewinnziels verwendet, das dem ursprünglichen EA entspricht.
- Die freie Marge wird anhand des verbundenen Portfolios (`CurrentValue - BlockedValue`) angenähert und fällt auf das aktuelle Eigenkapital zurück, wenn keine Margin-Daten verfügbar sind.
- Alle Laufzeitprotokollierungs-, Diagrammerstellungs- und Optimierungs-Hooks bleiben der Standardinfrastruktur von StockSharp überlassen, sodass die Strategie über den Designer optimiert oder direkt im API-Projekt ausgeführt werden kann.
