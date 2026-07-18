# RSI + EMA-Trendstrategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Dieses System kombiniert einen klassischen Relative Strength Index (RSI) Oszillator mit einem Doppel-Moving-Average-Trendfilter. Der RSI liefert kurzfristige Überkauft- und Überverkauft-Werte, während die beiden exponentiellen gleitenden Durchschnitte (EMAs) den übergeordneten Trend definieren. Die Strategie nimmt nur Trades in Richtung der schnellen EMA relativ zur langsamen EMA, um Gegentrend-Setups bei starken Richtungsbewegungen zu vermeiden.

Wenn der Kurs-Momentum RSI unter die Überverkauft-Schwelle drückt und die schnelle EMA über der langsamen EMA liegt, wird angenommen, dass der Markt in einem Aufwärtstrend ist und eine Long-Position eröffnet. Umgekehrt, wenn RSI über das Überkauft-Level steigt, während die schnelle EMA noch die langsame EMA überschreitet, initiiert die Strategie einen Short-Trade und erwartet einen kurzfristigen Rücksetzer innerhalb des größeren Trendkanals.

Positionen werden geschlossen, wenn RSI die Extremzone auf der entgegengesetzten Seite verlässt, was signalisiert, dass die Mean-Reversion-Bewegung wahrscheinlich erschöpft ist. Die Methode ist einfach, aber effektiv für das Erfassen kurzer Momentum-Schwingungen in Trendumgebungen. Sie funktioniert gut bei liquiden Instrumenten, wo RSI-Extreme häufig auftreten, aber die Trendrichtung intakt bleibt.

## Details

- **Einstiegskriterien**:
  - **Long**: `RSI < oversold` und `EMA1 > EMA2`.
  - **Short**: `RSI > overbought` und `EMA1 > EMA2`.
- **Long/Short**: Beide Seiten.
- **Ausstiegskriterien**:
  - **Long**: `RSI > overbought`.
  - **Short**: `RSI < oversold`.
- **Stops**: Keine eingebaut.
- **Standardwerte**:
  - `RSI Length` = 14.
  - `Overbought/Oversold` = 70 / 30.
  - `EMA Lengths` = 150 / 600.
- **Filter**:
  - Kategorie: Momentum
  - Richtung: Beide
  - Indikatoren: Mehrere
  - Stops: Nein
  - Komplexität: Grundlegend
  - Zeitrahmen: Intraday
  - Saisonalität: Nein
  - Neuronale Netze: Nein
  - Divergenz: Nein
  - Risikolevel: Mittel
