# RSI-Trend-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die **RSI-Trend-Strategie** verwendet den Relative Strength Index (RSI), um Trendumkehrungen zu erkennen, und verwaltet Positionen mit einem ATR-basierten Trailing-Stop. Das System eröffnet eine Long-Position, wenn der RSI einen überkauften Schwellenwert überschreitet, und tritt in eine Short-Position ein, wenn der RSI unter einen überverkauften Schwellenwert fällt. Das Risiko wird mit einem Trailing-Stop gesteuert, der aus der Average True Range (ATR) abgeleitet wird, sodass sich das Stop-Niveau an die aktuelle Volatilität anpassen kann.

Diese Implementierung ist zu Bildungszwecken konzipiert und zeigt, wie man eine hochrangige StockSharp-Strategie mithilfe von Indikator-Bindings aufbaut. Die Strategie handelt nur auf abgeschlossenen Kerzen und referenziert keine vorherigen Indikatorwerte direkt, was den StockSharp Best Practices entspricht.

## Details

- **Einstiegskriterien**:
  - **Long**: `RSI(t) > BuyLevel` und `RSI(t-1) <= BuyLevel`.
  - **Short**: `RSI(t) < SellLevel` und `RSI(t-1) >= SellLevel`.
- **Long/Short**: Beide Richtungen.
- **Ausstiegskriterien**:
  - Trailing-Stop basierend auf ATR-Vielfachem.
- **Stops**: Ja, dynamischer Trailing-Stop.
- **Standardwerte**:
  - `RSI Period` = 14.
  - `BuyLevel` = 73.
  - `SellLevel` = 27.
  - `ATR Period` = 100.
  - `ATR Multiple` = 3.
- **Filter**:
  - Kategorie: Trendfolge.
  - Richtung: Beide.
  - Indikatoren: RSI, ATR.
  - Stops: Ja.
  - Komplexität: Mittel.
  - Zeitrahmen: Beliebig (standardmäßig 1-Minuten-Kerzen).
  - Saisonalität: Nein.
  - Neuronale Netze: Nein.
  - Divergenz: Nein.
  - Risikolevel: Moderat.

