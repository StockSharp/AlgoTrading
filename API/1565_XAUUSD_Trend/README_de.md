# XAUUSD Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt XAUUSD mit EMA-Kreuzungen, RSI-Extremen und Bollinger Bands.
Eine Long-Position wird eröffnet, wenn die schnelle EMA die langsame EMA nach oben kreuzt, der RSI unter dem Überverkauft-Niveau liegt und der Preis über dem oberen Bollinger Band schließt.
Short-Positionen werden bei umgekehrten Bedingungen eröffnet.
Das Risikomanagement legt Stop-Loss- und Take-Profit-Niveaus auf Basis des Portfolio-Risikoprozentsatzes und eines Take-Profit-zu-Stop-Loss-Verhältnisses fest.

## Details

- **Einstieg**:
  - Long: schnelle EMA kreuzt langsame EMA nach oben, RSI < oversold, close > oberes Band.
  - Short: schnelle EMA kreuzt langsame EMA nach unten, RSI > overbought, close < unteres Band.
- **Ausstieg**: Stop-Loss oder Take-Profit aus den Risikoeinstellungen berechnet.
- **Indikatoren**: EMA, RSI, Bollinger Bands.
