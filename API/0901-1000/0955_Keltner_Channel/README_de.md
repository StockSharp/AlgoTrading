# Keltner Channel-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt Ausbrüche aus dem Keltner Channel und EMA-Trendkreuzungen.

## Details

- **Einstiegskriterien**:
  - Long: Kurs kreuzt das untere Keltner-Band nach unten oder EMA9 kreuzt EMA21 nach oben, während der Kurs über EMA50 liegt.
  - Short: Kurs kreuzt das obere Keltner-Band nach oben oder EMA9 kreuzt EMA21 nach unten, während der Kurs unter EMA50 liegt.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Kurs kreuzt das mittlere Band in entgegengesetzter Richtung oder die EMAs kreuzen sich zurück.
  - Stop-Loss bei 1.5 ATR.
  - Take-Profit bei 3 ATR.
- **Stops**: Ja.
- **Standardwerte**:
  - `Length` = 20
  - `Multiplier` = 1.5
  - `AtrMultiplier` = 1.5
  - `FastEmaPeriod` = 9
  - `SlowEmaPeriod` = 21
  - `TrendEmaPeriod` = 50
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filter**:
  - Kategorie: Kanal
  - Richtung: Beide
  - Indikatoren: EMA, ATR, Keltner
  - Stops: Ja
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
