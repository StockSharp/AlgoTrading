# DCA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kauft in regelmäßigen Abständen einen festen Quotierungswährungsbetrag, entweder an einem gewählten Wochentag oder nach einer festgelegten Anzahl von Kerzen. Optional werden alle Positionen nach einem angegebenen Enddatum geschlossen.

### Tatsächlicher Zeitplan und Volumen

Standardwerte: `CandleType=5m`, `CapitalToInvest=200` **Einheiten der Notierungswährung**, `StartDate=DateTime.MinValue`, `EndDate=DateTime.MaxValue`, `CloseAllOnLastCandle=false`, `UseDayOfWeek=false`, `DayOfWeek=Wednesday`, `EveryXCandles=72`.

Auf abgeschlossenen Kerzen mit `StartDate <= Kerzendatum < EndDate`: Intervallmodus kauft **alle 72 zulässigen Kerzen**; Wochentagsmodus kauft **auf der ersten zulässigen Kerze jedes Mittwochs**, höchstens einmal pro Datum. `200 / Close` wird auf den `Security.VolumeStep` **abgerundet**, unter Beachtung der Volumengrenzen. Es gibt nur kumulative **Markt-Käufe**, keine EMA-Signale und keine Shorts. Ab EndDate werden keine Käufe mehr ausgeführt. Mit `CloseAllOnLastCandle=true` wird die **tatsächliche Restposition** bei der ersten zulässigen Gelegenheit einmal geschlossen. Kein separater Preisstop; Slippage kann den Ausführungswert verändern. Die Zahlenwerte wurden hier als Defaults festgelegt, weil das ursprüngliche README sie nicht angab.



## Parameter
- Kerzentyp
- Kapital pro Handel
- Startdatum
- Enddatum
- Alles bei letzter Kerze schließen
- Wochentagsmodus verwenden
- Wochentag
- Alle X Kerzen
