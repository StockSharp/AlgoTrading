# Strategiediagramm mit Trendbestätigung des Referenzinstruments
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses Diagramm synchronisiert zuerst abgeschlossene Fünf-Minuten-Kerzen von BTCUSDT@BNBFT und TONUSDT@BNBFT und handelt danach exakte BTC-EMA-Kreuzungen, wenn der TON-EMA-Trend dieselbe Richtung bestätigt. Positions- und Pausenfilter prüfen jede Entscheidung, und zwei Market-Order-Pfade mit fester Menge steuern das Engagement.

![schema](schema.svg)

## Strategieübersicht

- Der Sync-Baustein erhält beide Ströme abgeschlossener Fünf-Minuten-Kerzen mit Interval `00:05:00` und aktiviertem ClearSockets. Er gibt ein ausgerichtetes BTC–TON-Paar nur aus, wenn beide Kerzen vorhanden sind; fehlt eine davon, wird das unvollständige Intervall verworfen.
- Jedes ausgerichtete Paar speist danach den schnellen EMA 7 und langsamen EMA 18 für BTC sowie den schnellen EMA 47 und langsamen EMA 50 für TON. Bei allen vier Indikatoren ist die Filterung ausschließlich fertig gebildeter Werte deaktiviert.
- Eine BTC-Aufwärtskreuzung erfordert `PrevFast <= PrevSlow` und `Fast > Slow`; eine Abwärtskreuzung erfordert `PrevFast >= PrevSlow` und `Fast < Slow`. Das aktuelle TON-Verhältnis bestätigt Käufe mit `Fast > Slow` und Verkäufe mit `Fast < Slow`.
- Der Kaufpfad erfordert zusätzlich `Position <= 0`, der Verkaufspfad dagegen `Position >= 0`. Beide Pfade senden `NoCondition`-Market-Orders mit festem Volume 1.
- Die ersten fünf synchronisierten BTC–TON-Paare werden blockiert, und jedes Ordersignal blockiert die nächsten fünf synchronisierten Paare; das sechste ausgerichtete Paar ist wieder zugelassen. Stop-Loss, Take-Profit und ein eigener Ausstiegsbaustein fehlen; der Chart zeigt BTC-Kerzen, beide BTC-EMAs und beide Ausführungsströme.

## Ein- und Ausstiegsregeln

- **Long-Einstieg**: Wenn BTC `PrevFast <= PrevSlow` und `Fast > Slow` erfüllt, TON aktuell `Fast > Slow` zeigt, die synchronisierte Positionsprüfung `Position <= 0` ergibt und die Pause beendet ist, sendet das Diagramm einen Market-Kauf mit Volume 1.
- **Short-Einstieg**: Wenn BTC `PrevFast >= PrevSlow` und `Fast < Slow` erfüllt, TON aktuell `Fast < Slow` zeigt, die synchronisierte Positionsprüfung `Position >= 0` ergibt und die Pause beendet ist, sendet das Diagramm einen Market-Verkauf mit Volume 1.
- **Ausstieg**: Es gibt keinen eigenen Ausstiegs- oder Schutzbaustein. Eine spätere zugelassene Order in Gegenrichtung reduziert das Engagement; bei einer Position von `+1` oder `-1` stellt das feste Volume 1 die Position glatt, statt die Gegenseite zu eröffnen.

## Parameter

| Parameter | Standard | Beschreibung |
|---|---|---|
| Main Fast EMA | 7 | Periode des schnellen ExponentialMovingAverage aus abgeschlossenen Fünf-Minuten-Kerzen von BTCUSDT@BNBFT; die Filterung ausschließlich fertig gebildeter Werte ist deaktiviert. |
| Main Slow EMA | 18 | Periode des langsamen ExponentialMovingAverage aus abgeschlossenen Fünf-Minuten-Kerzen von BTCUSDT@BNBFT; die Filterung ausschließlich fertig gebildeter Werte ist deaktiviert. |
| Reference Fast EMA | 47 | Periode des schnellen ExponentialMovingAverage aus der ausgerichteten abgeschlossenen Fünf-Minuten-Kerze von TONUSDT@BNBFT; die Filterung ausschließlich fertig gebildeter Werte ist deaktiviert. |
| Reference Slow EMA | 50 | Periode des langsamen ExponentialMovingAverage aus der ausgerichteten abgeschlossenen Fünf-Minuten-Kerze von TONUSDT@BNBFT; die Filterung ausschließlich fertig gebildeter Werte ist deaktiviert. |
| Cooldown Bars | 5 | Anzahl der anfänglichen und nach einem Signal blockierten synchronisierten Kerzenpaare, bevor das nächste ausgerichtete Paar zugelassen wird. |
| Volume | 1 | Feste Menge für beide NoCondition-Market-Order-Bausteine. |

## Diagrammdetails

- Zwei [Kerzen](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/data_sources/candles.html)-Bausteine leiten abgeschlossene Fünf-Minuten-Kerzen für BTCUSDT@BNBFT und TONUSDT@BNBFT direkt an die Synchronisation.
- Der [Sync](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/sync.html)-Baustein richtet beide Kerzeneingänge mit Interval `00:05:00` und ClearSockets `true` aus. Er gibt beide Kerzen als ein Paar frei; fehlt eine Seite, wird das unvollständige Intervall geleert, ohne die Indikatorkette zu erreichen.
- Nur das synchronisierte Paar speist vier [Indikator](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/indicator.html)-Bausteine: ExponentialMovingAverage 7 und 18 für BTC sowie 47 und 50 für TON. Ihre Option ausschließlich fertig gebildeter Werte ist `false`; nur die beiden BTC-EMA-Ausgaben werden zusätzlich an den Chart gesendet.
- [Vorheriger Wert](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/prev_value.html)-Bausteine halten die vorangegangenen synchronisierten schnellen und langsamen BTC-EMA-Werte. [Vergleich](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/comparison.html)-Bausteine bilden beide Seiten jeder exakten Kreuzung sowie die beiden aktuellen TON-Trendverhältnisse ab; getrennte Pfade der [Logikbedingung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/logical_condition.html) verbinden sie für Kauf und Verkauf.
- Die aktuelle [Position](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/current.html) liefert die Prüfungen `Position <= 0` und `Position >= 0`. Das [N values](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/delay_value.html)-Tor erhält die synchronisierte BTC-Kerzenausgabe und unterdrückt mit N=5 die ersten fünf ausgerichteten Paare sowie die fünf Paare nach jedem Ordersignal; beim sechsten gibt es Entscheidungen wieder frei.
- Die [Positionsänderung](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/positions/modify.html)-Bausteine für Kauf und Verkauf platzieren Market-Orders mit `NoCondition` und gemeinsamem Volume 1. Stop-Loss, Take-Profit, Positionsschutz und ein eigener Ausstiegsbaustein fehlen.
- Das [Chartpanel](https://doc.stocksharp.com/de/topics/designer/strategies/using_visual_designer/elements/common/chart.html) erhält abgeschlossene BTC-Kerzen, BTC EMA 7, BTC EMA 18 und die MyTrade-Ausgaben der Kauf- und Verkaufsbausteine.

## Verwendung

Importieren Sie die `.json`-Datei in Designer, testen Sie sie im Backtester mit historischen Daten und passen Sie danach Parameter oder Bausteine an Ihr Instrument an, bevor Sie live handeln.
