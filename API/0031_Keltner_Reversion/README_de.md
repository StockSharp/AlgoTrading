# Keltner Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Mean Reversion mit Keltner-Kanälen handelt

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 130%. Sie funktioniert am besten am Aktienmarkt.

Keltner Reversion handelt gegen Ausbrüche außerhalb des Keltner-Kanals. Einstiege setzen auf eine Rückkehr zur mittleren Bande, wobei Trades geschlossen werden, sobald der Preis wieder in den Kanal eintritt oder der Stop getroffen wird.

Die Kanalbreite dehnt sich mit der Volatilität aus und zusammen, sodass das System extreme Bewegungen erfassen kann, während es den Trades Raum zur Entwicklung lässt. Stops basieren typischerweise auf ATR-Vielfachen.


## Details

- **Einstiegskriterien**: Signale basierend auf RSI, ATR, Keltner.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Gegensätzliches Signal oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `StopLossAtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: RSI, ATR, Keltner
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (5m)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

