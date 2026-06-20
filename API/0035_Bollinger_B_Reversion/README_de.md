# Bollinger Percent B Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Dieser Ansatz handelt gegen Preisextreme jenseits der Bollinger Bands mit dem Percent B-Indikator. Bewegungen über das obere Band oder unter das untere Band deuten auf eine Überausdehnung hin.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 142%. Es funktioniert am besten auf dem Aktienmarkt.

Wenn Percent B kleiner als null oder größer als eins ist, setzt das System auf eine Rückkehr zur Mitte des Bandes. Ein Ausstiegsschwellenwert schließt Trades, sobald sich das Momentum normalisiert.

Stops werden bei einem festen Prozentsatz vom Einstieg gesetzt.

## Details

- **Einstiegskriterien**: Percent B außerhalb des Bereichs 0–1.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Percent B kreuzt `ExitValue` oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `ExitValue` = 0.5m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

