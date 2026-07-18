# Anchored-Momentum-Ausbruch-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Anchored-Momentum-Ausbruch-Strategie verwendet das Verhältnis eines Exponentiellen Gleitenden Durchschnitts (EMA) zu einem Einfachen Gleitenden Durchschnitt (SMA), um den Impuls zu messen. Wenn der kurzfristige EMA schneller zu steigen beginnt als der langfristige SMA, deutet dies auf einen zunehmenden bullischen Impuls hin. Umgekehrt signalisiert ein fallendes Verhältnis einen sich verstärkenden bärischen Impuls.

## Funktionsweise
1. **Indikatoren**
   - EMA mit konfigurierbarer Periode.
   - SMA mit konfigurierbarer Periode.
2. **Impulsberechnung**
   - `Momentum = 100 * (EMA / SMA - 1)`
   - Positiver Impuls bedeutet, EMA liegt über SMA; negativer Impuls bedeutet, EMA liegt unter SMA.
3. **Handelslogik**
   - Wenn der Impuls abgenommen hat und dann nach oben dreht, eröffnet die Strategie eine Long-Position.
   - Wenn der Impuls zugenommen hat und dann nach unten dreht, eröffnet die Strategie eine Short-Position.
   - Die Positionsgröße schließt automatisch die bestehende Position ein, um bei Bedarf zu drehen.
4. **Risikomanagement**
   - Stop-Loss- und Take-Profit-Level werden als Prozentsätze des Einstiegspreises mithilfe des integrierten Schutzmechanismus festgelegt.

## Parameter
| Name | Beschreibung |
|------|--------------|
| `SmaPeriod` | Periode für den SMA-Indikator. |
| `EmaPeriod` | Periode für den EMA-Indikator. |
| `StopLossPercent` | Prozentsatz für den Stop-Loss. |
| `TakeProfitPercent` | Prozentsatz für den Take-Profit. |
| `CandleType` | Zeitrahmen der für Berechnungen verwendeten Kerzen. |

## Hinweise
- Die Strategie arbeitet nur mit abgeschlossenen Kerzen.
- Alle Handelsaktionen werden mit Market-Orders ausgeführt.
- Indikatorwerte werden über die High-Level-`Bind`-API abgerufen, ohne direkt auf historische Puffer zuzugreifen.
