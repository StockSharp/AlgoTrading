# Five Ema NoTouch Breakout

[English](README.md) | [Русский](README_ru.md) | [Español](README_es.md) | [Português](README_pt.md) | [中文](README_zh.md) | [日本語](README_ja.md)

## Handelsregeln

Nur abgeschlossene Kerzen nach EMA-Bildung verwenden. Vollständig unter EMA: Long über dem Kerzenhoch vormerken; vollständig darüber: Short unter dem Tief. Die vorherige Vorgabe vor einer Ersetzung prüfen; Berührung genügt nicht. Nur aus leerer Position mit Volume einsteigen, während des Haltens Vorgabe löschen. Stop am gegenüberliegenden Extrem fixieren. Beim ersten tatsächlichen Einstieg den Abstand zum Stop als Risiko und RewardRisk×Risiko als Gewinnabstand festhalten. Nativer lokaler Marktschutz beobachtet Bid/Ask zwischen Kerzen. Eine Ausführung jenseits des Stops wird sofort glattgestellt. Keine Wartebalken oder Aufstockung.

## Parameter

| Parameter | Standard |
|---|---|
| `CandleType` | TimeSpan.FromMinutes(5).TimeFrame() |
| `EmaPeriod` | 5 |
| `RewardRisk` | 3 |

## Prüfung

Beide Sprachen werden mit gebündelten BTC/TON-Daten, unabhängigen Formeln, Orderseiten/-mengen, Ausführungen, Positionen und Ausstiegen geprüft. Datierte Preis-/Faktor- und Kalenderfixtures prüfen Mechanik; verschobene OHLC/Volumina sind keine mehrjährige Marktgeschichte. Zwei Ranggruppen, TopN=1 und SMA=2 sind ausdrücklich kleine Testprofile. Lange Originalfenster und Standardwerte werden separat geprüft. Keine Renditezusage.
