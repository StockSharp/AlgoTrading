# 2Mars OKX-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie kombiniert einen gleitenden Durchschnitt-Crossover mit einem SuperTrend-Filter. Bollinger Bands liefern Gewinnziele, während ein ATR-basierter Stop-Loss das Risiko begrenzt.

## Regeln
- **Long**: Signal-EMA kreuzt die Basis-EMA nach oben und der Preis liegt über dem SuperTrend.
- **Short**: Signal-EMA kreuzt die Basis-EMA nach unten und der Preis liegt unter dem SuperTrend.
- **Ausstieg**: Gewinnmitnahme am oberen oder unteren Bollinger Band, oder Stop-Loss beim ATR multipliziert mit einem Faktor.

## Indikatoren
- EMA
- SuperTrend
- Bollinger Bands
- Average True Range
