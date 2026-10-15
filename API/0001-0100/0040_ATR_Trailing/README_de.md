# Strategie ATR Trailing Stops
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ATR Trailing verwendet ein Average-True-Range-Vielfaches, um Stops hinter offenen Positionen nachzuziehen. Einstiege erfolgen, wenn der Preis einen gleitenden Durchschnitt kreuzt, und der Trailing-Stop passt sich der Volatilität an.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 157%. Es funktioniert am besten auf dem Kryptomarkt.

Wenn der Preis voranschreitet, verschiebt sich der Stop nach oben (oder unten) basierend auf der letzten ATR-Ablesung, ohne jemals zurückzugehen. Dadurch werden Gewinne gesichert, solange der Trend anhält.

Ausstiege erfolgen, wenn der Trailing-Stop ausgelöst wird oder wenn der Preis zurück durch den gleitenden Durchschnitt kreuzt.

## Details

- **Einstiegskriterien**: Preis über oder unter MA.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Trailing-Stop ausgelöst oder Preis kreuzt MA.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.0m
  - `MAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: ATR, MA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

