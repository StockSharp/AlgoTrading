# Strategie für die Hüllkurvenlimit-Leiter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **Envelope Limit Ladder Strategy** ist eine C#-Portierung des MetaTrader Expert Advisors `E_2_12_5min.mq4` (ID 7671). Es baut die ursprüngliche Leiter der Limit-Orders rund um einen EMA-Umschlag auf 5-Minuten-Kerzen neu auf und behält dabei das Multi-Target- und Trailing-Management-Modell des alten Roboters bei.

## Konzept

1. **Hüllkurvenfilter** – eine gleitende durchschnittliche Hüllkurve (Standardwert EMA 144 mit einer Abweichung von 0,05 %), berechnet auf dem konfigurierbaren Zeitrahmen `EnvelopeCandleType`, liefert die Mittellinie und die oberen/unteren Bänder.
2. **Signalkerze** – Handelssignale werden im `CandleType`-Abonnement ausgewertet (Standard 5 Minuten). Wenn die vorherige Kerze zwischen der Mittellinie und dem nächsten Band schließt, begrenzt die Strategie die Aufträge auf der Mittellinie.
3. **Auftragsleiter** – bis zu drei Kauflimits und drei Verkaufslimits werden gleichzeitig platziert:
   - Einstiegspreis: ausgerichteter Mittellinienwert.
   - Stop-Loss: entgegengesetztes Hüllkurvenband.
   - Take-Profit: Band ± benutzerdefinierte Offsets (standardmäßig 8, 13 und 21 Punkte).
4. **Handelsfenster** – ausstehende Aufträge werden nur erstellt, wenn `TradingStartHour < Hour < TradingEndHour`. Alle verbleibenden Limits werden aufgehoben, sobald die Öffnungszeit `TradingEndHour` erreicht.
5. **Positionsverwaltung** – jede ausgeführte Limit-Order platziert sofort ihre eigene Stop- und Take-Profit-Order. Ein optionaler Trailing-Modus verschiebt den Stop auf den gleitenden Durchschnitt (oder hält ihn auf dem gegenüberliegenden Band), wenn der Preis über die Hülle hinaus ausbricht.

## Parameter

| Parameter | Standard | Beschreibung |
|-----------|---------|-------------|
| `CandleType` | 5 Minuten | Kerzentyp zur Signalerkennung. |
| `EnvelopeCandleType` | 5 Minuten | Kerzentyp, der zur Berechnung der Hülle verwendet wird. Verwenden Sie einen längeren Zeitrahmen, um die MT4-Eingabe `EnvTimeFrame` nachzuahmen. |
| `EnvelopePeriod` | 144 | Gleitende durchschnittliche Länge des Umschlags. |
| `MaMethod` | EMA | Methode des gleitenden Durchschnitts (`SMA`, `EMA`, `SMMA`, `LWMA`). |
| `EnvelopeDeviation` | 0,05 | Umschlagbreite in Prozent (0,05 = 0,05 %). |
| `TradingStartHour` | 0 | Erste Stunde, in der ausstehende Aufträge angezeigt werden können (exklusive Prüfung, entspricht MT4-Verhalten). |
| `TradingEndHour` | 17 | Stunde, in der alle ausstehenden Aufträge entfernt werden (exklusive Obergrenze). |
| `FirstTakeProfitPoints` | 8 | Versatz in Punkten, der über die Hüllkurve hinaus für die erste Leitersprosse hinzugefügt wird. |
| `SecondTakeProfitPoints` | 13 | Versatz in Punkten für die zweite Sprosse. |
| `ThirdTakeProfitPoints` | 21 | Versatz in Punkten für die dritte Sprosse. |
| `UseOppositeEnvelopeTrailing` | `true` | Hält den Stop auf dem gegenüberliegenden Band (`true`) oder verfolgt ihn zum gleitenden Durchschnitt (`false`). Spiegelt das MT4-Flag `MaElineTSL` wider. |
| `OrderVolume` | 0,1 | Volumen pro ausstehender Bestellung (ersetzt die adaptive Losgröße von MT4). |

## Verhaltenshinweise

- Die Strategie verwaltet für jede ausgeführte Limit-Order ein separates Stop/Take-Paar. Ausgänge behindern nicht die übrigen Sprossen der Leiter.
- Das Trailing wird erst nach einem Ausbruch über die Hülle hinaus aktiviert und verschärft den Stop nur in die profitable Richtung.
- Wenn `EnvelopeCandleType` von `CandleType` abweicht, werden die aktuellsten Umschlagwerte des sekundären Abonnements für Signalkerzen wiederverwendet, was weitgehend der MT4-Umschlagsuche für höhere Zeitrahmen entspricht.
- Die ursprüngliche MT4-Geldverwaltungsroutine (`LotsOptimized`) wird durch den expliziten Parameter `OrderVolume` ersetzt, um den Port innerhalb von StockSharp deterministisch zu halten.

## Nutzungstipps

- Passen Sie den Hüllkurven-Zeitrahmen an die MT4-Eingaben an, um das ursprüngliche Verhalten zu reproduzieren (z. B. `EnvelopeCandleType` bei 5 Minuten belassen oder je nach Bedarf auf 1 Stunde/4 Stunden wechseln).
- Setzen Sie `UseOppositeEnvelopeTrailing` auf `false`, wenn Sie möchten, dass der Trailing Stop zum gleitenden Durchschnitt und nicht zum entgegengesetzten Band springt, sobald der Preis den Umschlag verlässt.
- Optimieren Sie die Take-Profit-Offsets und die Umschlagsabweichung gemeinsam. Die Leiterabstände hängen von der durch die Hülle erfassten Volatilität ab.
