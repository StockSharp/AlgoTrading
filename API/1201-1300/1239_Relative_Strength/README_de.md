# Relative-Stärke-Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Diese Strategie berechnet ein gewichtetes relatives Stärkemaß aus mehreren gleitenden Durchschnitten.
Bollinger Bands am Stärkesignal zeigen überkaufte und überverkaufte Zonen an.
Die Strategie kauft, wenn die Stärke über das obere Band steigt, und verkauft, wenn sie unter das untere Band fällt.

## Details

- **Einstieg**: Stärke kreuzt über das obere Band für Long, unter das untere Band für Short.
- **Ausstieg**: entgegengesetzter Bandkreuzung.
- **Indikatoren**: EMA 8, EMA 34, SMA 20, SMA 50, SMA 200, Bollinger Bands.
- **Typ**: Momentum.
