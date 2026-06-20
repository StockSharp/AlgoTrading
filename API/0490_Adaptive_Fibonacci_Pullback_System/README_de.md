# Adaptive Fibonacci-Pullback-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie mittelt drei SuperTrend-Linien, die mit Fibonacci-Multiplikatoren (0.618, 1.618, 2.618) konstruiert wurden, und glättet das Ergebnis mit einer EMA. Trades folgen Pullbacks zu diesem adaptiven Trend, während eine AMA-basierte Mittellinie und ein optionaler RSI-Filter die Richtung bestätigen.

## Details

- **Einstiegskriterien**:
  - Tief unterhalb des gemittelten SuperTrend und Schluss oberhalb seines geglätteten Wertes.
  - Der vorherige Schluss relativ zur AMA-Mittellinie definiert den Pullback.
  - **Long**: Schluss über der Mittellinie und RSI > Schwellenwert.
  - **Short**: Schluss unter der Mittellinie und RSI < Schwellenwert.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - Schluss, der den geglätteten SuperTrend in entgegengesetzter Richtung kreuzt.
- **Stops**: Prozentualer Stop-Loss und Take-Profit über `StartProtection`.
- **Standardwerte**:
  - `AtrPeriod` = 8
  - `SmoothLength` = 21
  - `AmaLength` = 55
  - `RsiLength` = 7
  - `RsiBuy` = 70
  - `RsiSell` = 30
  - `TakeProfitPercent` = 5
  - `StopLossPercent` = 0.75
- **Filter**:
  - Kategorie: Trend-Pullback
  - Richtung: Beide
  - Indikatoren: SuperTrend, EMA, AMA, RSI
  - Stops: Ja
  - Komplexität: Moderat
  - Zeitrahmen: Beliebig
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
