# Стратегия RSI Buy Sell Force
[English](README.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Стратегия вычисляет RSI и сглаживает его EMA.
Из этих значений формируются линии `cc` и `bb`, отражающие силу покупателей и продавцов.
Длинная позиция открывается при пересечении `cc` выше `bb`, короткая — при пересечении ниже.
