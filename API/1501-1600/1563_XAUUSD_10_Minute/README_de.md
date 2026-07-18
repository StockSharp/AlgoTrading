# XAUUSD 10-Minuten-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie handelt XAUUSD auf 10-Minuten-Kerzen mithilfe von MACD-, RSI- und Bollinger-Bands-Signalen. Sie eröffnet Long-Positionen bei bullischen Bedingungen und Short-Positionen bei bärischen Signalen. Das System wendet ATR-basierte Stop-Loss- und Take-Profit-Niveaus an, die um einen festen Spread bereinigt sind.

## Details

- **Einstiegskriterien**:
  - **Long**: MACD-Linie kreuzt über die Signallinie, RSI unter Überverkauft oder Preis unter dem unteren Bollinger Band.
  - **Short**: MACD-Linie kreuzt unter die Signallinie, RSI über Überkauft oder Preis über dem oberen Bollinger Band.
- **Long/Short**: Beide.
- **Ausstiegskriterien**:
  - Position geschlossen bei entgegengesetztem Signal, Stop-Loss oder Take-Profit.
- **Stops**: ATR-Stop-Loss bei `3 * ATR`, Take-Profit bei `5 * ATR`.
- **Standardwerte**:
  - MACD fast/slow/signal: `12/26/9`.
  - RSI period: `14`, overbought `65`, oversold `35`.
  - Bollinger length `20`, width `2`.
  - ATR period `14`.
  - Spread `38` ticks.
- **Filter**:
  - Kategorie: Trendfolge
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Intraday
