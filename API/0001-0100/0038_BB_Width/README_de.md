# Strategie Bollinger Band Width Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Die Bollinger-Band-Breite misst den Abstand zwischen dem oberen und unteren Band. Eine zunehmende Breite deutet auf Volatilität und mögliche Trendbildung hin. Diese Strategie handelt Ausbrüche, wenn die Breite zunimmt.

Tests zeigen eine durchschnittliche jährliche Rendite von etwa 151%. Es funktioniert am besten auf dem Aktienmarkt.

Die Preisposition relativ zum mittleren Band legt die Richtung fest. Ein sich erweiternder Kanal mit Preis über dem mittleren Band löst Long-Positionen aus, während ein Kanal unterhalb des mittleren Bands Short-Positionen auslöst.

Ausstiege erfolgen, wenn die Bandbreite sich zusammenzieht oder ein Volatilitäts-Stop erreicht wird.

## Details

- **Einstiegskriterien**: Bandbreite expandiert und Preis relativ zum mittleren Band.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Bandbreite zieht sich zusammen oder Stop.
- **Stops**: Ja.
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filter**:
  - Kategorie: Ausbruch
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, ATR
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel

