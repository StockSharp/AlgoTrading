# MACD mit 1D Stochastic Bestätigungs-Umkehr-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die beim Kreuzen der MACD-Linie über die Signallinie mit Bestätigung durch den täglichen Stochastic-Oszillator kauft. Der Handel wird geschlossen, wenn der Preis einen ATR-basierten Stop-Loss erreicht oder unter einen Trailing-EMA-Take-Profit fällt.

## Details

- **Einstiegskriterien**:
  - Long: `MACD crosses above Signal && DailyK > DailyD && DailyK < 80`
- **Long/Short**: Nur Long
- **Stops**: ATR Stop-Loss und Trailing-EMA-Take-Profit
- **Standardwerte**:
  - `MacdFastLength` = 12
  - `MacdSlowLength` = 26
  - `MacdSignalLength` = 9
  - `TrailingEmaLength` = 20
  - `StopLossAtrMultiplier` = 3.25m
  - `TrailingActivationAtrMultiplier` = 4.25m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filter**:
  - Kategorie: Umkehr
  - Richtung: Long
  - Indikatoren: MACD, Stochastic, ATR, EMA
  - Stops: Ja
  - Komplexität: Mittel
  - Zeitrahmen: Mittelfristig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
