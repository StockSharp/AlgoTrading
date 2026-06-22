# Rabbit M2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Überblick
Rabbit M2 ist eine Trendfolge-Strategie, die Momentum-Oszillatoren, Donchian-Ausbrüche und adaptive Positionsgrößen kombiniert. Die ursprüngliche MetaTrader 5-Version von Peter Byrom wechselt zwischen Kauf- und Verkaufsregimen basierend auf exponentiellen gleitenden Durchschnitten (EMAs) auf einem höheren Zeitrahmen. Innerhalb des aktiven Regimes wartet die Strategie auf Williams %R-Schwingungen, die durch den Commodity Channel Index (CCI) bestätigt werden, bevor eine Position eröffnet wird. Positionen werden mit festen Stop-Loss- und Take-Profit-Zielen geschützt und zwangsweise geschlossen, wenn der Preis die entgegengesetzte Donchian-Kanalgrenze verletzt. Nach jedem profitablen Ausstieg oberhalb eines konfigurierbaren Gewinnziels erhöht die Strategie ihre Basisauftragsgröße und verdoppelt den Gewinnziel-Schwellenwert, wodurch die Skalierungslogik des MQL-Expertenberaters nachgeahmt wird.

## Indikatoren und Marktdaten
- **Schnelle EMA (40) und langsame EMA (80)** berechnet auf 1-Stunden-Kerzen, steuern die Handelsrichtung und schließen Trades bei Regimewechseln.
- **Commodity Channel Index (14)** gemessen auf dem primären Zeitrahmen, bestätigt überkauften oder überverkauften Momentum.
- **Williams %R (50)** auf dem primären Zeitrahmen gibt den Auslöser, wenn er die -20/-80-Niveaus kreuzt.
- **Donchian-Kanal (100)** abgeleitet vom primären Zeitrahmen, definiert Ausbruchsausstiege, wenn der Preis das vorherige 100-Balken-Hoch oder -Tief durchbricht.
- **Fester Stop-Loss und Take-Profit** werden 50 Pips vom Einstiegspreis entfernt gesetzt (Pip-Größe passt sich an 3/5-stellige Instrumente an).

Zwei Datenströme sind erforderlich: der konfigurierbare primäre Zeitrahmen für CCI/Williams %R/Donchian-Berechnungen und ein dedizierter 1-Stunden-Strom für den EMA-Trendfilter.

## Handelsregeln
### Regimekontrolle
1. Wenn der 40-Perioden-EMA auf dem H1-Feed unter den 80-Perioden-EMA fällt, werden alle Long-Positionen geschlossen und nur Short-Setups sind erlaubt.
2. Wenn der 40-Perioden-EMA über den 80-Perioden-EMA steigt, werden alle Short-Positionen geschlossen und nur Long-Setups sind erlaubt.

### Einstiegskriterien
- **Short-Einstieg**
  - Williams %R fällt unter -20, während der vorherige Wert zwischen -20 und 0 lag.
  - CCI liegt über dem Verkaufsniveau (Standard 101).
  - Short-Regime ist aktiv und das aktuelle Nettoppositionsvolumen liegt unter dem `MaxOpenPositions`-Limit.
- **Long-Einstieg**
  - Williams %R steigt über -80, während der vorherige Wert zwischen -100 und -80 lag.
  - CCI liegt unter dem Kaufniveau (Standard 99).
  - Long-Regime ist aktiv und das aktuelle Nettoppositionsvolumen liegt unter dem `MaxOpenPositions`-Limit.

Bei jedem Einstieg schließt die Strategie gegenläufige Exposition (falls vorhanden) und öffnet die neue Position mit dem aktuellen Basisvolumen.

### Ausstiegskriterien
1. Stop-Loss und Take-Profit werden bei jeder abgeschlossenen Kerze bewertet: Longs steigen aus, wenn das Tief den Stop kreuzt oder das Hoch das Ziel erreicht; Shorts verhalten sich umgekehrt.
2. Unabhängig von Stop/Ziel steigen Shorts aus, wenn der Preis über das vorherige 100-Balken-Hoch schließt, und Longs, wenn der Preis unter das vorherige 100-Balken-Tief schließt.
3. Ein Regimewechsel (schnelle EMA kreuzt die langsame EMA) liquidiert sofort bestehende Exposition.

### Positionsgrößen-Logik
- Das Basisauftragsvolumen beginnt bei `InitialVolume` (Standard 0.01) und folgt den Börsenlimits (Schritt/Min/Max).
- Nach jedem realisierten Gewinn größer als `BigWinTarget` erhöht sich das Basisvolumen um `VolumeStep` und der Schwellenwert verdoppelt sich, wodurch das kaskadierende Wachstumsmuster des ursprünglichen Expertenberaters erhalten bleibt.
- Der Parameter `MaxOpenPositions` begrenzt das Netto-Engagement. Im StockSharp-Port werden Positionen verrechnet, sodass das Erreichen des Limits bedeutet, dass kein zusätzliches Volumen hinzugefügt wird, bis das Engagement sinkt.

## Parameter
| Name | Standard | Beschreibung |
| --- | --- | --- |
| `CciSellLevel` | 101 | Mindestwert des CCI, der zur Bestätigung eines Short-Setups erforderlich ist. |
| `CciBuyLevel` | 99 | Höchstwert des CCI, der zur Bestätigung eines Long-Setups erforderlich ist. |
| `CciPeriod` | 14 | Periode des Commodity Channel Index auf dem primären Zeitrahmen. |
| `DonchianPeriod` | 100 | Rückblickperiode für den Donchian-Kanal in der Ausstiegslogik. |
| `MaxOpenPositions` | 1 | Maximal erlaubte Nettoppositions-Vielfache des Basisvolumens. |
| `BigWinTarget` | 1.50 | Gewinn (in Kontowährung) zum Skalieren des Volumens erforderlich. |
| `VolumeStep` | 0.01 | Inkrement, das zum Basisvolumen nach einem qualifizierenden Gewinn hinzugefügt wird. |
| `WprPeriod` | 50 | Länge des Williams %R Oszillators. |
| `FastEmaPeriod` | 40 | Schnelle EMA-Periode im 1-Stunden-Trend-Feed. |
| `SlowEmaPeriod` | 80 | Langsame EMA-Periode im 1-Stunden-Trend-Feed. |
| `TakeProfitPips` | 50 | Abstand des Take-Profits in Pips. |
| `StopLossPips` | 50 | Abstand des Stop-Losses in Pips. |
| `InitialVolume` | 0.01 | Startauftragsvolumen vor Skalierungsregeln. |
| `CandleType` | 15-Minuten-Kerzen | Primärer Zeitrahmen für CCI/Williams %R/Donchian-Berechnungen. |

## Implementierungshinweise
- Der StockSharp-Port emuliert MT5-Stop-Loss und Take-Profit durch Überwachung von Kerzen-Hochs/-Tiefs statt durch Platzierung broker-gebundener Orders.
- Preisschritte und Pip-Berechnungen passen sich automatisch an 3- oder 5-Dezimalinstrumente an, indem die gemeldete Tick-Größe mit 10 multipliziert wird.
- Die Strategie stützt sich auf realisierte PnL-Updates, um «große Gewinne» zu erkennen; stellen Sie sicher, dass Trades an die Strategie zurückgemeldet werden, damit die Skalierung funktioniert.
