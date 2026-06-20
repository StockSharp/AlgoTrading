# ATR Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ATR Reversion sucht nach plötzlichen Bewegungen, gemessen in Vielfachen des Average True Range (ATR). Wenn der Preis den ATR-Multiplikator überschreitet, erwartet das System eine Mean Reversion.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 133%. Es funktioniert am besten auf dem Kryptomarkt.

Die Strategie eröffnet einen Trade entgegen der Richtung des Spikes und verwendet einen gleitenden Durchschnitt zur Beurteilung des Momentums.

Positionen schließen bei einem Gleitenden-Durchschnitt-Crossover oder wenn der Volatilitäts-Stop erreicht wird.

## Details

- **Einstiegskriterien**: Preisbewegung überschreitet `AtrMultiplier` mal ATR.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Preis kreuzt gleitenden Durchschnitt oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: ATR, MA
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

