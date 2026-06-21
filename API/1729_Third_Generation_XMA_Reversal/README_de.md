# XMA-Umkehr-Strategie der 3. Generation
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet einen doppelt geglätteten exponentiellen gleitenden Durchschnitt, bekannt als XMA der 3. Generation, um lokale Hochs und Tiefs zu identifizieren. Eine Long-Position wird eröffnet, wenn der XMA von einem lokalen Tief nach oben dreht. Shorts werden initiiert, wenn der XMA von einem lokalen Hoch umkehrt. Positionen werden bei entgegengesetzten Signalen umgekehrt, und es wird kein expliziter Stop oder Take Profit verwendet.

## Details
- **Einstiegskriterien**: Der XMA bildet ein lokales Minimum oder Maximum und kehrt um.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**: Entgegengesetztes Signal.
- **Stops**: Keine.
- **Standardwerte**:
  - `MaLength` = 50
  - `CandleType` = TimeSpan.FromHours(4)
- **Filter**:
  - Kategorie: Trend
  - Richtung: Beide
  - Indikatoren: EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday (4H)
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
