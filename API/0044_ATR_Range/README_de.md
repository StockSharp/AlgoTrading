# ATR Range Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ATR Range Breakout misst die Preisbewegung über eine feste Anzahl von Bars und vergleicht sie mit der durchschnittlichen wahren Spanne. Wenn die Bewegung die ATR überschreitet, wird eine Position in Richtung der Bewegung eröffnet.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 169%. Am besten funktioniert es im Kryptomarkt.

Die Strategie prüft den Preis alle N Bars und verwendet den gleitenden Durchschnitt für Ausstiegssignale. Sie zielt darauf ab, Momentum zu erfassen, wenn die Volatilität über das normale Niveau hinaus steigt.

Trades schließen, wenn der Preis wieder durch den gleitenden Durchschnitt kreuzt oder wenn der auf ATR basierende Stop ausgelöst wird.

## Details

- **Einstiegskriterien**: Preis bewegt sich über den Lookback-Zeitraum um mehr als ATR.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kreuzt MA oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `LookbackPeriod` = 5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: ATR, MA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

