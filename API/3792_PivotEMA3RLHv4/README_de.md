# PivotEMA3RLHv4-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick

PivotEMA3RLHv4 ist eine Trendfolgestrategie, die das tägliche Pivot-Level mit kurzfristigen Momentum-Filtern kombiniert. Es verfolgt einen exponentiellen gleitenden Durchschnitt über drei Perioden (EMA), der anhand der Eröffnungskurse der Kerzen berechnet wird, und vergleicht ihn mit demselben EMA, der anhand der Schlusskurse berechnet wurde. Das Setup wird mit Heiken-Ashi-Kerzen validiert, um die Richtung zu bestätigen, und mit mehreren Messungen des Average True Range (ATR), um sicherzustellen, dass die Volatilität zunimmt. Die Strategie handelt mit einem einzelnen Instrument im ausgewählten Intraday-Zeitrahmen und wartet immer auf das Ende der aktuellen Kerze, bevor sie eine Entscheidung trifft.

## Handelslogik

1. **Pivot-Filter** – Der vorherige EMA(3) des Eröffnungspreises muss unter (für Long-Positionen) oder über (für Short-Positionen) dem täglichen Pivot-Level liegen, während der aktuelle EMA(3) des Eröffnungskurses auf die gegenüberliegende Seite des Pivot-Levels gehen muss.
2. **Heiken Ashi-Bestätigung** – Die aktuelle Heiken Ashi-Kerze muss für Long-Positionen bullisch (Schlusskurs über Eröffnung) oder für Short-Positionen bärisch (Schlusskurs unter Eröffnung) sein.
3. **Momentum Check** – Der auf Schlusskursen basierende EMA(3) muss den EMA bei Eröffnungen in Handelsrichtung vorantreiben.
4. **Volatilitätsausweitung** – Mindestens einer der Werte ATR(4), ATR(8), ATR(12) oder ATR(24) muss im Vergleich zur vorherigen Kerze ansteigen, und die True Range (ATR mit der Länge 1) muss entweder auf diesem Balken zunehmen oder auf dem vorherigen Balken gestiegen sein.
5. **Positionsverwaltung** – Es ist jeweils nur eine Position aktiv. Schutzstopps und -ziele werden intern simuliert und bei Erreichen über Marktaufträge ausgeführt.

Ausstiegssignale spiegeln die Einstiegsregeln wider: Wenn die gegenteiligen Bedingungen eintreten, schließt die Strategie den aktuellen Handel. Darüber hinaus können die optionalen Stop-Loss-, Take-Profit- und Trailing-Stop-Mechanismen einen Trade früher schließen.

## Parameter

| Parameter | Beschreibung |
| --- | --- |
| `CandleType` | Arbeitszeitrahmen für die Strategiekerzen. |
| `StopLossPips` | Anfänglicher Stoppabstand in Pips vom Einstiegspreis. Zum Deaktivieren auf Null setzen. |
| `TakeProfitPips` | Gewinnzielentfernung in Pips. Zum Deaktivieren auf Null setzen. |
| `UseTrailingStop` | Aktiviert oder deaktiviert die Trailing-Stop-Verwaltung. |
| `TrailingStopType` | Trailing-Modus: 1 behält einen festen Abstand bei, 2 wird aktiviert, nachdem sich der Preis um `TrailingStopPips` bewegt hat, 3 verwendet die unten beschriebene mehrstufige Leiter. |
| `TrailingStopPips` | Entfernung (in Pips), die vom Trailing-Typ 2 verwendet wird. |
| `FirstMovePips` / `FirstStopLossPips` | Triggerabstand und daraus resultierender Stopp-Offset für die erste Stufe des Trailing-Typs 3. |
| `SecondMovePips` / `SecondStopLossPips` | Triggerabstand und daraus resultierender Stopp-Offset für die zweite Stufe des Trailing-Typs 3. |
| `ThirdMovePips` / `TrailingStop3Pips` | Auslösedistanz und dynamische Nachlaufdistanz für die Endstufe des Nachlauftyps 3. |

## Trailing-Stop-Modi

- **Typ 1** – Positioniert den Stopp so neu, dass er dem Preis nie mehr als die anfängliche Stoppdistanz hinterherhinkt.
- **Typ 2** – Wartet darauf, dass sich der Preis um `TrailingStopPips` bewegt, bevor Gewinne im gleichen Abstand gesichert werden.
- **Typ 3** – Verwendet bis zu drei Schwellenwerte: Die ersten beiden verschieben den Stopp auf vordefinierte Offsets, während der dritte in einen regulären Trailing-Stop umgewandelt wird.

## Notizen

- Die Strategie abonniert tägliche Kerzen, um das Pivot-Level aus den Höchst-, Tiefst- und Schlusskursen des Vortages zu berechnen.
- Indikatoren werden innerhalb des Candle-Handlers nur anhand fertiger Balken aktualisiert, wodurch die Logik sowohl mit Online- als auch mit Backtesting-Umgebungen kompatibel bleibt.
- Die ursprüngliche MetaTrader-Version stützte sich auf Stopps auf Brokerseite; Dieser Port simuliert sie und beendet sie bei Bedarf mit Marktaufträgen.
