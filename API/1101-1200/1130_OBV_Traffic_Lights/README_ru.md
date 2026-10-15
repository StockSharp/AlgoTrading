# Стратегия OBV Traffic Lights
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия использует OBV на основе свечей Heikin Ashi и три EMA, окрашенные как светофор. Лонг открывается, когда OBV и быстрая EMA выше медленной EMA; шорт — когда OBV и быстрая EMA ниже медленной. Позиция закрывается при исчезновении условий.

- **Условия входа**: OBV > медленной EMA и быстрая EMA > медленной EMA; OBV < медленной EMA и быстрая EMA < медленной EMA.
- **Условия выхода**: противоположный сигнал или потеря согласования.
- Индикаторы: OBV, EMA, Highest/Lowest
