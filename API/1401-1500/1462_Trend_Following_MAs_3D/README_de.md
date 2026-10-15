# Trendfolge-Strategie mit MAs 3D
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet zwei kurze einfache gleitende Durchschnitte, um die Trendrichtung zu erkennen.
Eine Long-Position wird eröffnet, wenn der 5-Perioden-Durchschnitt über dem 10-Perioden-Durchschnitt liegt.
Eine Short-Position wird eröffnet, wenn das Gegenteil der Fall ist.

## Details

- **Einstieg**:
  - **Long**: SMA(5) > SMA(10)
  - **Short**: SMA(5) < SMA(10)
- **Ausstieg**: umgekehrtes Signal
- **Indikatoren**: SMA
- **Zeitrahmen**: konfigurierbar
- **Typ**: Trendfolge
