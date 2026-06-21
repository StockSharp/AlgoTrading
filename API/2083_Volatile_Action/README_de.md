# Volatile-Action-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen kurzfristigen Volatilitätsausbruch mit Bill Williams' **Alligator**-Trendfilter, der auf dem 4-Stunden-Zeitrahmen berechnet wird.

## Handelsregeln
- **Long-Einstieg** wenn:
  - Der ATR mit Periode 1 größer ist als *Volatility Coef* mal dem ATR mit Periode *ATR Period*.
  - Die Kerze bullisch ist und ein neues 24-Balken-Hoch setzt.
  - Die Alligator-Linien nach oben ausgerichtet sind (Lips > Teeth > Jaw) und sowohl Eröffnung als auch Schlusskurs über der Teeth-Linie liegen.
- **Short-Einstieg** wenn die obigen Bedingungen in entgegengesetzter Richtung gespiegelt sind.

Beim Einstieg setzt die Strategie Stop-Loss- und Take-Profit-Levels als Vielfache des ATR(1):
- Stop-Loss = Einstiegspreis ± *Stop Coef* × ATR(1)
- Take-Profit = Einstiegspreis ± *Profit Coef* × ATR(1)

## Parameter
- **Volatility Coef** – Multiplikator zum Vergleich des schnellen ATR mit dem langsamen ATR.
- **ATR Period** – Periode des langsamen ATR.
- **Stop Coef** – ATR-Multiplikator für den Stop-Loss.
- **Profit Coef** – ATR-Multiplikator für den Take-Profit.
- **Candle Type** – Zeitrahmen für die Hauptanalyse (Alligator verwendet 4H-Kerzen).
