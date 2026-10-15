# Post-Open Long ATR Stop Loss Take Profit-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie eröffnet eine Long-Position während der Marktöffnung nach einem Ausbruch aus dem Widerstand, während der Kurs nahe der Bollinger-Mittellinie bleibt. Sie verwendet EMA-, RSI-, ADX- und ATR-Filter und verlässt die Position über ATR-basierten Stop-Loss und Take-Profit.

## Details

- **Einstiegskriterien**:
  - **Long**: Ausbruch über den jüngsten Widerstand während der Marktöffnung, Kurs nahe der Bollinger-Mittellinie, RSI über dem Schwellenwert, ADX über dem Schwellenwert, kurzfristiger Aufwärtstrend und kein Rücksetzer.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**:
  - ATR-basierter Stop-Loss oder Take-Profit erreicht.
- **Stops**:
  - ATR-basierter Stop-Loss und Take-Profit.
- **Standardwerte**:
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
  - `SessionStart` = 09:30 UTC
  - `SessionDuration` = TimeSpan.FromHours(1)
  - `ResistancePeriod` = 5
  - `BasisProximity` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Long
  - Indikatoren: Bollinger Bands, EMA, RSI, ADX, ATR
  - Stops: ATR
  - Komplexität: Mittel
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

## Berechnung und Ausführung

Signale verwenden abgeschlossene Kerzen nach Bildung aller Indikatoren. Widerstand ist das höchste Hoch der vorigen `ResistancePeriod` Kerzen ohne die aktuelle; Einstieg erfordert `Close > resistance`. Das Öffnungsfenster verwendet UTC-Kerzenanfangszeiten: Beginn inklusive, Ende exklusiv, mit Mitternachtsüberlauf. `SessionStart` ist auf die Öffnungszeit des Instruments einzustellen.

Bollinger-Mitte ist der Schlusskurs-SMA einschließlich der aktuellen Kerze; Abweichung ist die Populationsstandardabweichung. Nähe: `abs(Close - basis) <= (UpperBand - basis) * BasisProximity`. Erforderlich sind `kurze EMA > lange EMA` und `Close > kurze EMA`. Kein Rücksetzer bedeutet `Close >= Open` der Signalkerze. RSI und ADX müssen strikt über ihren Schwellen liegen.

Kauf von `Volume` nur ohne Position und ohne offene Order. Wilder ATR der Einstiegskerze fixiert Stop- und Take-Abstände. Nativer lokaler Schutz setzt die Ebenen unter/über dem tatsächlichen Ausführungspreis (volumengewichteter Preis bei Teilausführungen), beobachtet Level1-Best-Bid und Kerzenpreise und schließt den gesamten verbleibenden Long. Ausstiege bleiben außerhalb des Öffnungsfensters aktiv. Spätere ATR-Änderungen verschieben die Ebenen nicht. Keine Shorts, EMA-Kreuzungsausstiege oder Wartefrist.
