# Nacht-Scalper-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt rund um die Abendsession mit Bollinger-Bändern. Positionen werden nur nach einer bestimmten Startstunde eröffnet, wenn die Bandbreite schmal ist und der Kurs außerhalb der Bänder ausbricht.

## Details

- **Einstiegskriterien**:
  - **Long**: nach `Start Hour` schließt der Kurs unterhalb des unteren Bollinger-Bandes und die Bandbreite ist kleiner als `Range Threshold`.
  - **Short**: nach `Start Hour` schließt der Kurs oberhalb des oberen Bollinger-Bandes und die Bandbreite ist kleiner als `Range Threshold`.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Position wird geschlossen, wenn die Zeit vor `Start Hour` des nächsten Tages liegt.
  - Schützender Stop-Loss und Take-Profit werden durch `StartProtection` verwaltet.
- **Stops**: Verwendet `StartProtection` mit festen Stop-Loss- und Take-Profit-Abständen.
- **Standardwerte**:
  - `BB Period` = 40
  - `BB Deviation` = 1
  - `Range Threshold` = 450
  - `Stop Loss` = 370
  - `Take Profit` = 20
  - `Start Hour` = 19
  - `Candle Type` = 1h
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Beide
  - Indikatoren: Bollinger Bands
  - Stops: Ja
  - Komplexität: Niedrig
  - Zeitrahmen: Kurzfristig
