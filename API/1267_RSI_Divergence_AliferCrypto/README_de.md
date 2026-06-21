# RSI-Divergenz-Strategie - AliferCrypto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie basierend auf RSI-Divergenzen mit optionalen Zonen- und Trendfiltern. Stop-Loss und Take-Profit können aus Swings oder ATR berechnet werden, mit dynamischen oder statischen Aktualisierungen.

## Logik
- **Einstieg**
  - Bullische Divergenz: Der Kurs bildet ein tieferes Tief, während der RSI ein höheres Tief bildet.
  - Bärische Divergenz: Der Kurs bildet ein höheres Hoch, während der RSI ein niedrigeres Hoch bildet.
  - Der optionale RSI-Zonenfilter erfordert einen vorherigen überkauften/überverkauften Zustand.
  - Der optionale Trendfilter verwendet die Richtung des gleitenden Durchschnitts.
- **Ausstieg**
  - SL/TP aus dem letzten Swing oder ATR.
  - Niveaus können beim Einstieg fixiert oder bei jeder Kerze neu berechnet werden.

## Indikatoren
- Relative Strength Index
- Moving Average
- Average True Range
- Highest/Lowest
