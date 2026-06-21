# Multi-Schritt FlexiMA-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Verwendet einen gleitenden Durchschnitts-Oszillator mit variabler Länge mit einem SuperTrend-Filter und mehrstufigem Take-Profit.

- **Long** wenn der Preis über der SuperTrend-Linie liegt und der Oszillator positiv ist.
- **Short** wenn der Preis unter der SuperTrend-Linie liegt und der Oszillator negativ ist.
- **Teilausstiege** auf drei Take-Profit-Niveaus.
- **Schließen** der verbleibenden Position, wenn die entgegengesetzte Bedingung eintritt.

**Indikatoren**: Variabler SMA-Oszillator, SuperTrend
**Stops**: nur mehrstufiger Take-Profit
