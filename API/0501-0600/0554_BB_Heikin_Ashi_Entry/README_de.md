# Bollinger Heikin Ashi Einstiegs-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die Bollinger Bands auf Heikin Ashi Kerzen anwendet. Kauft nach zwei aufeinanderfolgenden bearischen Heikin Ashi Kerzen, die die untere Band berühren, gefolgt von einer bullischen Kerze darüber. Verkauft in umgekehrter Richtung.

Nach dem Einstieg wird ein erstes Ziel in Höhe des Risikos genommen und der Stop wird mit den Extremen der vorherigen Kerze nachgezogen.

## Details

- **Einstiegskriterien**:
  - Long: zwei bearische HA-Kerzen, die die untere Band berühren, dann bullisch darüber
  - Short: zwei bullische HA-Kerzen, die die obere Band berühren, dann bearisch darunter
- **Long/Short**: Beide
- **Ausstiegskriterien**:
  - Long: erstes Ziel bei 1R, dann Trailing-Stop an vorherigen Tiefs
  - Short: erstes Ziel bei 1R, dann Trailing-Stop an vorherigen Hochs
- **Stops**: Tief/Hoch der vorherigen Kerze
- **Standardwerte**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Beide
  - Indikatoren: Bollinger Bands, Heikin Ashi
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
