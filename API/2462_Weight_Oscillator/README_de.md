# Gewichteter Oszillator-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert RSI, Money Flow Index, Williams %R und DeMarker zu einem gewichteten Oszillator, der durch einen einfachen gleitenden Durchschnitt geglättet wird. Positionen werden geöffnet oder umgekehrt, wenn der Oszillator konfigurierbare hohe oder niedrige Niveaus kreuzt. Der Trendmodus bestimmt, ob Trades den Oszillatorsignalen folgen oder dagegen handeln.

## Details

- **Einstiegskriterien**:
  - **Trend = Direct**:
    - **Long**: Oszillator fällt unter das niedrige Niveau.
    - **Short**: Oszillator steigt über das hohe Niveau.
  - **Trend = Against**:
    - **Long**: Oszillator steigt über das hohe Niveau.
    - **Short**: Oszillator fällt unter das niedrige Niveau.
- **Long/Short**: Beide.
- **Ausstiegskriterien**: Gegenüberliegende Kreuzung kehrt die Position um.
- **Stops**: Keine expliziten Stops.
- **Filter**: Gewichteter Oszillator mit SMA-Glättung.
