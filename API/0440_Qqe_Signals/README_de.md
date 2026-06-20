# QQE Signals-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementiert die Quantitative Qualitative Estimation-Technik auf RSI. Der Indikator baut dynamische obere und untere Bänder um eine geglättete RSI-Linie auf und verfolgt Bandkreuzungen, um Trendwechsel zu signalisieren. Wenn RSI über das Trailing-Band kreuzt, wird ein Long-Signal generiert; Kreuzungen darunter lösen Ausstiege aus.

Durch Anpassung der Bänder an die Volatilität versucht QQE, Rauschen zu glätten und dabei reaktionsfähig zu bleiben. Die Strategie konzentriert sich auf Long-Trades und verlässt sich auf die Trade-Umkehrungen des Motors zum Schließen von Positionen.

## Details

- **Einstiegskriterien**:
  - **Long**: Geglättete RSI-Linie kreuzt über das Trailing-Band.
- **Ausstiegskriterien**:
  - RSI fällt unter das gegenüberliegende Band oder ein entgegengesetztes Signal erscheint.
- **Indikatoren**:
  - RSI (Periode 14, Glättung 5)
  - QQE-Bänder abgeleitet vom ATR des RSI mit Faktor 4.238
- **Stops**: Standardmäßig keine; verlässt sich auf entgegengesetzte Signale.
- **Standardwerte**:
  - `RsiPeriod` = 14
  - `RsiSmoothing` = 5
  - `QqeFactor` = 4.238
  - `Threshold` = 10
- **Filter**:
  - Trendfolge
  - Einzelner Zeitrahmen
  - Indikatoren: RSI, QQE
  - Stops: Keine
  - Komplexität: Moderat
