# Dualer Strategie-Selektor V2 - Cryptogyani-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie wechselt zwischen zwei SMA-basierten Long-only-Ansätzen.

- **Strategie 1**: Handelt SMA-Crossover mit optionalem Trailing-Take-Profit oder festem Ziel.
- **Strategie 2**: Handelt SMA-Crossover bestätigt durch Trend auf höherem Zeitrahmen, verwendet ATR-Stop und partiellen Take-Profit.

## Details

- **Einstiegskriterien**:
  - Strategie 1: Schneller SMA kreuzt über langsamen SMA.
  - Strategie 2: Schneller SMA kreuzt über langsamen SMA und Preis liegt über dem SMA des höheren Zeitrahmens.
- **Ausstiegskriterien**:
  - Strategie 1: Take-Profit-Ziel oder Trailing-Stop.
  - Strategie 2: Partieller Take-Profit, dann ATR-basierter Stop.
- **Indikatoren**: SMA, ATR.
- **Richtung**: Nur Long.
- **Stops**: Ja.
