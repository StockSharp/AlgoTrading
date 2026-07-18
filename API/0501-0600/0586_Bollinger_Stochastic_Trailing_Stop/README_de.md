# Bollinger und Stochastic Trailing Stop-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie eröffnet eine Long-Position, wenn der Preis unterhalb des unteren Bollinger Bands schließt und Stochastic %K unter 20 liegt. Eine Short-Position wird eröffnet, wenn der Preis oberhalb des oberen Bands schließt und %K über 80 liegt. Ein ATR-basierter Trailing Stop schützt offene Positionen.

## Details
- **Einstiegskriterien:**
  - **Long:** close < unteres Bollinger Band und %K < 20.
  - **Short:** close > oberes Bollinger Band und %K > 80.
- **Long/Short:** Beide.
- **Ausstiegskriterien:** ATR-basierter Trailing Stop.
- **Stops:** Trailing Stop basierend auf ATR * Multiplikator.
- **Standardwerte:** Bollinger-Länge = 20, Abweichung = 2, Stochastic-Länge = 14, Glättung = 3, ATR-Periode = 14, ATR-Multiplikator = 1.5.
