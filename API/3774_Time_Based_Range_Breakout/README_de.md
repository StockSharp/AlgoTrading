# Zeitbasierte Range-Breakout-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Diese Strategie ist eine direkte Portierung des MetaTrader 4 Expert Advisors `Tttttt_www_forex-instruments_info.mq4`. Es erstellt einmal pro Tag zu einem konfigurierbaren Zeitpunkt Intraday-Breakout-Level. Immer wenn der Preis über diesen Niveaus schließt, eröffnet die Strategie eine Position in Ausbruchsrichtung. Exits werden durch dynamische Gewinn- und Verlustdistanzen gesteuert, die aus einem Durchschnitt historischer Tagesbereiche abgeleitet werden.

## Kernlogik
1. **Tägliche Snapshot-Zeit** – Bei `CheckHour:CheckMinute` friert die Strategie die Höchst- und Tiefststände des aktuellen Tages ein und schließt alle offenen Positionen.
2. **Berechnung des durchschnittlichen Bereichs** – Der Algorithmus aggregiert die letzten `DaysToCheck`-Statistiken:
   - *CheckMode = 1*: nutzt den gesamten Hoch-/Tief-Bereich jedes abgeschlossenen Tages.
   - *CheckMode = 2*: Verwendet die absolute Differenz zwischen den Prüfzeitabschlüssen aufeinanderfolgender Tage.
3. **Level-Konstruktion** – Der Durchschnittswert wird durch `OffsetFactor` dividiert, um ein oberes und unteres Ausbruchsband um das Hoch/Tief des aktuellen Tages zu erstellen. Der gleiche Durchschnitt wird durch `ProfitFactor` und `LossFactor` dividiert, um dynamische Take-Profit- und Stop-Distanzen abzuleiten.
4. **Eintrittsfenster** – Nach dem täglichen Schnappschuss beobachtet die Strategie, dass die Kerze bis 23:00 Uhr schließt. Wenn ein Schlusskurs das obere Band durchbricht und keine Position offen ist, wird gekauft; Wenn das untere Band durchbrochen wird, wird es verkauft. Die Anzahl der Einträge pro Tag ist auf `TradesPerDay` begrenzt.
5. **Exit-Management** – Während Sie sich in einer Position befinden, vergleicht die Strategie den Schlusskurs mit dem durchschnittlichen Einstiegspreis (`Strategy.PositionPrice`). Sobald die Bewegung dafür oder dagegen die konfigurierten Gewinn- oder Verlustabstände erreicht, wird die Position zum Marktwert geschlossen. Bei `CloseMode = 2` werden alle verbleibenden Positionen ebenfalls zu Beginn des nächsten Handelstages geschlossen.

## Parameter
| Name | Beschreibung | Standard |
| --- | --- | --- |
| `CheckHour` | Stunde (0–23), in der der tägliche Bereichs-Snapshot erstellt wird. | `8` |
| `CheckMinute` | Minute (0-59), in der der Schnappschuss aufgenommen wird. | `0` |
| `DaysToCheck` | Anzahl der historischen Tage, die für die Mittelwertbildung verwendet werden. | `7` |
| `CheckMode` | `1` = tägliche Höchst-/Tiefstspanne verwenden, `2` = absolute Differenz zwischen aufeinanderfolgenden Prüfzeitabschlüssen verwenden. | `1` |
| `ProfitFactor` | Dividiert den gemittelten Wert, um die Gewinnzielentfernung zu erhalten. | `2` |
| `LossFactor` | Dividiert den gemittelten Wert, um die Verlustdistanz zu erhalten. | `2` |
| `OffsetFactor` | Dividiert den gemittelten Wert, um den Ausbruchsversatz um Hoch/Tief zu erhalten. | `2` |
| `CloseMode` | `1` = Positionen über Nacht halten, `2` = reduzieren, wenn sich der Kalendertag ändert. | `1` |
| `TradesPerDay` | Maximal zulässige Anzahl an Einträgen pro Tag. | `1` |
| `CandleType` | Für alle Berechnungen verwendete Kerzenserie (standardmäßig 15-Minuten-Kerzen). | `15m` Zeitrahmen |

Alle Parameter werden über `Strategy.Param` erstellt, sodass sie die Optimierung sofort unterstützen.

## Unterschiede zur MQL-Version
- MetaTrader verfolgt variable Gewinne direkt; Der StockSharp-Port rekonstruiert es aus `Position` und `PositionPrice`, wenn Exits ausgewertet werden.
- Der MT4-Code zählte aktive Bestellungen über Ticketschleifen. Der Port verwendet `TradesPerDay` zusammen mit der aggregierten Position, um die Anzahl der Transaktionen am selben Tag unter Kontrolle zu halten.
- Das ursprüngliche Skript stützte sich auf historische Puffer (z. B. `Highest`, `Lowest`). Die StockSharp-Version speichert tägliche Statistiken intern, vermeidet explizite Indikatorpuffer und respektiert gleichzeitig die übergeordneten API-Richtlinien.
- Schützende Stop-Loss- und Take-Profit-Orders wurden zusammen mit dem Markteintritt in MT4 gesendet. Der Hafen führt eine entsprechende Risikokontrolle durch, indem er Kerzenschließungen überwacht und Marktausstiegsaufträge sendet, wenn Schwellenwerte erreicht werden.

## Nutzungshinweise
- Verwenden Sie eine Kerzenserie, die der Balkengröße des ursprünglichen MQL-Setups entspricht (in der Referenzdatei wurden 15-Minuten-Balken verwendet).
- Stellen Sie mindestens `DaysToCheck` abgeschlossene Tage an historischen Daten bereit, bevor Sie mit der Strategie beginnen, andernfalls bleiben Ausbruchsniveaus inaktiv.
- Halten Sie bei der Optimierung die Faktoren positiv, um sinnvolle Ausbruchs- und Risikoschwellen beizubehalten.
