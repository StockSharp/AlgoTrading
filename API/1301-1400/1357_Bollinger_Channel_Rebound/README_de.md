# Bollinger-Kanal-Rückprall-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Konvertiert aus dem TradingView-Skript "strategy1". Die Strategie handelt Rückpraller im Bollinger-Kanal. Sie eröffnet eine Long-Position, nachdem der Preis unter das untere Band fällt und dann darüber schließt. Ausstiege werden durch das Kreuzen über das mittlere Band, das Berühren des oberen Bandes oder einen Stop-Loss unterhalb des Kanals ausgelöst.

## Details

- **Einstiegskriterien**: Preis war unter dem unteren Band und schließt dann darüber.
- **Long/Short**: Nur Long.
- **Ausstiegskriterien**: Kreuzung über das mittlere Band, Berührung des oberen Bandes oder Stop-Loss unterhalb des Kanals.
- **Stops**: Ja, fester Stop unterhalb des Kanals.
- **Standardwerte**:
  - `Length` = 20
  - `BufferFactor` = 0.2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Long
  - Indikatoren: Bollinger Bands
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Variabel
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
