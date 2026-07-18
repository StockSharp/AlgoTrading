# ATR Sell-the-Rip Mean-Reversion-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Nur-Short-Strategie, die verkauft, wenn der Preis über einen geglätteten ATR-Schwellenwert ansteigt, und bei einem Rückgang unter das vorherige Tief eindeckt. Ein optionaler EMA-Filter beschränkt Trades auf Abwärtstrends.

## Details

- **Einstiegskriterien**: Schlusskurs über dem geglätteten (close + ATR * Multiplikator)
- **Long/Short**: Short
- **Ausstiegskriterien**: Schlusskurs unter dem vorherigen Tief
- **Stops**: Nein
- **Standardwerte**:
  - `AtrPeriod` = 20
  - `AtrMultiplier` = 1.0
  - `SmoothPeriod` = 10
  - `EmaPeriod` = 200
- **Filter**:
  - Kategorie: Mean Reversion
  - Richtung: Short
  - Indikatoren: ATR, SMA, EMA
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
