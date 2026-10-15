# Zakryvator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Zakryvator-Strategie ist ein Risikomanagement-Modul, das die aktuelle offene Position überwacht und sie schließt, wenn der unrealisierte Verlust einen vordefinierten Schwellenwert überschreitet. Der zulässige Verlust hängt vom Positionsvolumen ab und repliziert die Logik des originalen MQL-Skripts, bei dem verschiedene Lotgrößen unterschiedlichen maximalen Drawdowns entsprechen.

Diese Strategie generiert selbst keine Einstiege. Positionen werden voraussichtlich manuell oder von einer anderen Strategie eröffnet. Zakryvator schützt das Konto einfach, indem es Verlustpositionen automatisch schließt.

## Details

- **Einstiegskriterien**: Keine. Die Strategie verwaltet nur bestehende Positionen.
- **Ausstiegskriterien**: Schließt die aktuelle Position, sobald der Verlust den konfigurierten Schwellenwert für ihr Volumen erreicht.
- **Long/Short**: Beide Richtungen werden unterstützt.
- **Stops**: Verwendet feste monetäre Verlustlimits, die mit der Positionsgröße variieren.
- **Filter**: Keine zusätzlichen Filter.

## Parameter

| Parameter | Beschreibung |
|-----------|--------------|
| `Min001002` | Maximaler Verlust für Positionen mit Volumen ≤ 0.02 Lots. |
| `Min002005` | Maximaler Verlust für Positionen mit Volumen zwischen 0.02 und 0.05 Lots. |
| `Min00501` | Maximaler Verlust für Positionen mit Volumen zwischen 0.05 und 0.10 Lots. |
| `Min0103` | Maximaler Verlust für Positionen mit Volumen zwischen 0.10 und 0.30 Lots. |
| `Min0305` | Maximaler Verlust für Positionen mit Volumen zwischen 0.30 und 0.50 Lots. |
| `Min051` | Maximaler Verlust für Positionen mit Volumen zwischen 0.50 und 1 Lot. |
| `MinFrom1` | Maximaler Verlust für Positionen mit Volumen größer als 1 Lot. |

## Verhalten

1. Die Strategie abonniert Trade-Ticks, um Preise in Echtzeit zu verfolgen.
2. Bei jedem Tick berechnet sie den unrealisierten PnL anhand des aktuellen Preises und des durchschnittlichen Einstiegspreises.
3. Wenn der Verlust den Schwellenwert für das aktuelle Positionsvolumen überschreitet, wird die Position zum Marktpreis geschlossen.

Dies macht Zakryvator zu einem einfachen, aber effektiven Werkzeug zur Begrenzung von Drawdowns basierend auf der Handelsgröße.
