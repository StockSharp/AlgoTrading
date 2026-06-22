# RoBoost-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie ist eine C#-Adaption des ursprünglichen MQL4-Expertenberaters **RoBoostj**.
Sie handelt ein einzelnes Instrument unter Verwendung RSI-basierter Signale kombiniert mit
einfacher Preismomentum-Erkennung. Die Strategie arbeitet auf einem ausgewählten Kerzentyp
(Standard: 1-Stunden-Kerzen).

## Logik

- Wenn der vorherige Schlusskurs höher als der aktuelle ist und der RSI-Wert unter den
  **RSI Down**-Schwellenwert fällt, eröffnet die Strategie eine Short-Position.
- Wenn der vorherige Schlusskurs niedriger oder gleich dem aktuellen ist und der RSI-Wert
  über den **RSI Up**-Schwellenwert steigt, eröffnet die Strategie eine Long-Position.
- Aktive Positionen werden mit folgenden Risikoinstrumenten verwaltet:
  - Feste **Take Profit**- und **Stop Loss**-Niveaus in Preiseinheiten.
  - Optionaler Trailing Stop, der aktiviert wird, wenn der Trade um die **Trail Start**-
    Distanz ins Plus läuft. Nach Aktivierung folgt der Stop-Preis dem Preis um die
    **Trail Step**-Distanz.

## Parameter

| Name            | Beschreibung                                                  |
|-----------------|---------------------------------------------------------------|
| `CandleType`    | Kerzenserie für Berechnungen.                                 |
| `RsiPeriod`     | Periodenlänge des RSI-Indikators.                             |
| `RsiUp`         | RSI-Schwellenwert für Long-Einstiege.                         |
| `RsiDown`       | RSI-Schwellenwert für Short-Einstiege.                        |
| `TakeProfit`    | Take-Profit-Abstand vom Eintrittspreis (Punkte).              |
| `StopLoss`      | Stop-Loss-Abstand vom Eintrittspreis (Punkte).                |
| `UseTrailing`   | Aktiviert die Trailing-Stop-Logik.                            |
| `TrailStart`    | Abstand in Punkten, ab dem der Trailing Stop aktiv wird.      |
| `TrailStep`     | Abstand in Punkten, der vom aktuellen Preis gehalten wird,
                   wenn der Trailing Stop aktiv ist.                               |

Alle Abstände werden in absoluten Preiseinheiten ausgedrückt und müssen je nach
Tick-Größe des Instruments angepasst werden.

## Verwendung

1. Füge die Strategie zu deinem Projekt hinzu oder öffne sie im StockSharp Designer.
2. Konfiguriere die Parameter nach deinen Handelspräferenzen.
3. Starte die Strategie. Sie abonniert automatisch die gewählte Kerzenserie und
   verwaltet Trades basierend auf RSI-Werten und Kerzenschlusskursen.

Die Strategie ist für Ausbildungszwecke gedacht und sollte an historischen Daten
getestet werden, bevor sie auf Live-Märkten eingesetzt wird.
