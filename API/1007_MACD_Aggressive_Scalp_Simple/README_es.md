# Estrategia de Scalping Agresivo Simple con MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa una estrategia de scalping usando el histograma del MACD con un filtro EMA de 50 períodos.

- Va largo cuando el histograma del MACD cruza por encima de cero y el precio está por encima de la EMA.
- Va corto cuando el histograma cruza por debajo de cero y el precio está por debajo de la EMA.
- Cierra posiciones cuando el momentum del histograma se revierte.
