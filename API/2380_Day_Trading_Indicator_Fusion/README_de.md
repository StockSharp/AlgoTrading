# Indikator-Fusion-Strategie für den Tageshandel
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Die Strategie handelt 5-Minuten-Kerzen mit Parabolic SAR, MACD (12,26,9), Stochastic Oscillator (5,3,3) und Momentum (14). Sie erfordert die Ausrichtung aller Indikatoren, bevor eine Position eröffnet wird.

- **Long-Einstieg**: SAR unterhalb des Preises, vorheriger SAR oberhalb des aktuellen, Momentum < 100, MACD-Linie unterhalb der Signallinie, Stochastic %K < 35.
- **Short-Einstieg**: SAR oberhalb des Preises, vorheriger SAR unterhalb des aktuellen, Momentum > 100, MACD-Linie oberhalb der Signallinie, Stochastic %K > 60.

Positionen werden geschlossen, wenn die entgegengesetzten Bedingungen eintreten. Das Risikomanagement verwendet einen Trailing-Stop und optionalen Take-Profit.

## Parameter
- **Volume** – Ordervolumen.
- **Take Profit** – Gewinnziel in Punkten.
- **Trailing Stop** – Trailing-Stop-Abstand in Punkten.
- **Candle Type** – Kerzen-Abonnement-Typ (Standard: 5 Minuten).
