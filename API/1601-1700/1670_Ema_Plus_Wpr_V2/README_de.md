# EMA plus WPR v2 Strategie
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Strategie, die den Williams %R Oszillator mit einem EMA-Trendfilter kombiniert. Handelt, wenn WPR nach einer Korrektur extreme Levels erreicht. Enthält optionale WPR-basierte Ausstiege, Trailing Stops und einen barbasierten Ausstieg.

## Details

- **Long**: WPR erreicht -100 nach einer Korrektur und der EMA-Trend ist aufwärts gerichtet.
- **Short**: WPR erreicht 0 nach einer Korrektur und der EMA-Trend ist abwärts gerichtet.
- **Indikatoren**: Williams %R, EMA.
- **Stops**: fester Stop-Loss und Take-Profit, optionaler Trailing Stop.
