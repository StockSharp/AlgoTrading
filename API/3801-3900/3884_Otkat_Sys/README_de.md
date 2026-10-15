# Otkat Sys-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie reproduziert den Expertenberater MetaTrader **1_Otkat_Sys**. Es überwacht den Eröffnungs-, Schluss- und Höchststand des vorherigen Handelstages.
und niedrig, um zu entscheiden, ob in den ersten drei Minuten nach Mitternacht (Brokerzeit) von Dienstag bis eine Position eingegeben werden soll
Donnerstag.

## Handelslogik

1. **Tägliche Statistiken** – die letzte abgeschlossene tägliche Kerze wird zwischengespeichert, um Folgendes zu berechnen:
   - `Open - Close` und `Close - Open`, um festzustellen, ob die vorherige Sitzung bärisch oder bullisch war.
   - `Close - Low` und `High - Close`, um zu messen, wie stark sich der Preis von den Extremen zurückgezogen hat.
2. **Einstiegsfenster** – neue Trades werden ausgewertet, wenn die Einstiegskerze zwischen 00:00 und 00:03 Uhr geöffnet wird. Montag und Freitag sind
übersprungen, passend zu den `DayOfWeek`-Filtern des ursprünglichen Roboters.
3. **Richtungsfilter** – vier sich gegenseitig ausschließende Bedingungen spiegeln die MQL-Regeln wider:
   - Bärischer Vortag (`Open - Close` über der Korridorschwelle) kombiniert mit einem flachen Retracement (`Close - Low`
unter `Pullback - Tolerance`) öffnet eine lange.
   - Der bullische Vortag mit einem ausgedehnten Aufwärts-Retracement (`High - Close` über `Pullback + Tolerance`) eröffnet ebenfalls eine Long-Position.
   - Der bullische Vortag mit einem schwachen Aufwärts-Retracement (`High - Close` unter `Pullback - Tolerance`) eröffnet einen Short.
   - Der rückläufige Vortag mit einem ausgedehnten Abwärts-Retracement (`Close - Low` über `Pullback + Tolerance`) eröffnet einen Short.
4. **Orders** – Eingaben sind Market Orders, die mit der konfigurierten Losgröße platziert werden. Kaufgeschäfte verwenden eine Take-Profit-Distanz von
`TakeProfit + 3` Punkte (wie im Original EA); Shorts verwenden genau `TakeProfit` Punkte. Beide Seiten wenden den gleichen Stop-Loss an
Entfernung.
5. **Zeitbasierter Ausstieg** – jede offene Position wird nach 22:45 Uhr reduziert, wodurch die nächtliche Bereinigung nachgebildet wird, die im MetaTrader implementiert wurde.
Skript.

Alle Schwellenwertparameter werden in Punkten ausgedrückt und mit dem `PriceStep` des Instruments in Preisabstände übersetzt.

## Parameter

| Name | Beschreibung |
| --- | --- |
| `EntryCandleType` | Zeitrahmen für das Handelsfenster (Standard: 1 Minute). |
| `DailyCandleType` | Zeitrahmen zur Bereitstellung der täglichen Statistiken (Standard: 1 Tag). |
| `TakeProfit` | Gewinnziel in Punkten. Long-Trades fügen einen 3-Punkte-Puffer hinzu. |
| `StopLoss` | Schutzstoppabstand in Punkten. |
| `PullbackThreshold` | Basis-Pullback-Schwellenwert („Otkat“) in Punkten. |
| `CorridorThreshold` | Schwellenwert für den Richtungskorridor (`KoridorOC`). |
| `ToleranceThreshold` | Rückzugstoleranz (`KoridorOt`). |
| `TradeVolume` | Losgröße für jeden Eintrag. |

Die Strategie setzt ihre zwischengespeicherten Werte am `Reset` automatisch zurück, abonniert sowohl Eintritts- als auch tägliche Kerzenströme und Ziehungen
Kerzen plus Handelsmarkierungen, wenn ein Chartbereich verfügbar ist.
