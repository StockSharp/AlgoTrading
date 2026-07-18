# Forex Fraus M1 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Die Forex Fraus M1-Strategie repliziert den MetaTrader 5-Expert-Advisor "Forex Fraus M1" im StockSharp-Framework. Es ist ein konträres System, das einen Williams %R-Oszillator mit langem Rückblick (Periode 360) auf Ein-Minuten-Kerzen überwacht. Immer wenn der Oszillator extreme Werte berührt, versucht die Strategie, die Bewegung zu verblassen, mit dem Ziel einer schnellen Reversion zum Mittelpunkt des jüngsten Bereichs. Die Implementierung behält das ursprüngliche Money-Management des Experten bei, einschließlich optionaler Handelszeiten, statischer Stop-Loss- und Take-Profit-Levels in Pips und einem pip-basierten Trailing Stop.

## Handelslogik
- **Indikator**: Williams %R mit einem 360-Perioden-Rückblick.
- **Kaufsignal**: Wenn Williams %R unter `-99.9` fällt, gilt der Markt als extrem überverkauft. Die Strategie sendet eine Market-Kauforder, wenn keine Long-Position vorhanden ist. Wenn `CloseOppositePositions` aktiviert ist, wird jedes Short-Engagement in derselben Orderanforderung geschlossen.
- **Verkaufssignal**: Wenn Williams %R über `-0.1` steigt, ist der Markt extrem überkauft. Die Strategie gibt eine Market-Verkaufsorder aus und schließt optional zuerst jedes offene Long-Engagement.
- **Zeitfilter**: Wenn `UseTimeControl` aktiviert ist, wertet die Strategie Signale nur zwischen `StartHour` (inklusiv) und `EndHour` (exklusiv) aus. Wenn die Sitzung Mitternacht überspannt (`StartHour > EndHour`), ist der Handel von `StartHour` bis 23 und von 0 bis `EndHour - 1` erlaubt.

## Risikomanagement
- **Stop-Loss**: Berechnet als `StopLossPips * PipSize` unterhalb (für Longs) oder oberhalb (für Shorts) des Einstiegspreises. Wenn das Kerzentief das Stop-Level berührt, wird die Position zum Markt geschlossen.
- **Take-Profit**: Berechnet als `TakeProfitPips * PipSize` oberhalb (für Longs) oder unterhalb (für Shorts) des Einstiegspreises. Wenn das Kerzenhoch/-tief dieses Level erreicht, wird die Position zur Gewinnsicherung geschlossen.
- **Trailing Stop**: Wenn sowohl `TrailingStopPips` als auch `TrailingStepPips` positiv sind, wird der Stop gestrafft, sobald der Preis sich mindestens `TrailingStopPips + TrailingStepPips` Pips zugunsten des Trades bewegt. Für Longs folgt der Stop dem Schluss minus `TrailingStopPips`; für Shorts folgt er dem Schluss plus `TrailingStopPips`.
- **Pip-Größe**: `PipSize` definiert den monetären Wert eines Pips. Für fünfstellige Forex-Symbole setzen Sie `PipSize` auf `0.0001`, für dreistellige JPY-Paare verwenden Sie `0.01` usw.

Die Strategie überprüft Stop-Loss- und Take-Profit-Bedingungen anhand von Kerzenhochs/-tiefs. Wenn beide innerhalb derselben Kerze berührt werden, hat der Schutz-Stop Vorrang, was das konservative Verhalten des ursprünglichen Experten widerspiegelt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `OrderVolume` | `0.1` | Handelsvolumen für neue Positionen. |
| `StopLossPips` | `50` | Stop-Loss-Abstand in Pips vom Einstiegspreis. Auf null setzen zum Deaktivieren. |
| `TakeProfitPips` | `150` | Take-Profit-Abstand in Pips vom Einstiegspreis. Auf null setzen zum Deaktivieren. |
| `TrailingStopPips` | `1` | Basis-Trailing-Stop-Abstand in Pips. Auf null setzen zum Deaktivieren des Trailings. |
| `TrailingStepPips` | `1` | Mindest-Pip-Gewinn vor Bewegung des Trailing Stops. |
| `UseTimeControl` | `true` | Aktiviert den Intraday-Sitzungsfilter. |
| `StartHour` | `7` | Startstunde für die Handelssitzung (0-23). |
| `EndHour` | `17` | Endstunde für die Handelssitzung (1-24, exklusiv). |
| `CloseOppositePositions` | `true` | Bei Aktivierung werden bestehende Positionen in einer einzigen Order umgekehrt. |
| `WilliamsPeriod` | `360` | Rückblickperiode für den Williams %R-Indikator. |
| `CandleType` | `1 minute` | Kerzentyp zur Bewertung von Williams %R und Handelsregeln. |
| `PipSize` | `0.0001` | Wert eines einzelnen Pips in Preiseinheiten. |

## Zusätzliche Hinweise
- Die Strategie verwendet StockSharps High-Level-Kerzen-Abonnement-API und Indikator-Bindung für präzise Logik ohne manuelle Buffer-Verwaltung.
- Stop-Loss-, Take-Profit- und Trailing-Berechnungen erfolgen auf abgeschlossenen Kerzen, um nicht auf unvollständige Preisdaten zu reagieren.
- Die Implementierung ruft `StartProtection()` einmalig beim Start auf, um den Projektrichtlinien zu entsprechen, während das eigentliche Risiko-Management innerhalb der Strategielogik verwaltet wird.
- Passen Sie den `PipSize`-Parameter an das gehandelte Instrument an, damit Pip-basierte Abstände korrekt auf Preisbewegungen abgebildet werden.
