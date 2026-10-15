# Estrategia de Bill Williams Wise Man 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa el segundo patrón "sabio" del sistema de trading de Bill Williams.
Analiza el histograma del Awesome Oscillator (AO) para detectar cambios de impulso:

- **Compra** cuando el AO está por encima de cero y forma un pico seguido de tres barras consecutivamente más bajas.
- **Venta** cuando el AO está por debajo de cero y forma un valle seguido de tres barras consecutivamente más altas.

Cada vez que aparece una señal, la estrategia cierra la posición opuesta y abre una nueva en la
dirección de la señal. Por defecto se utilizan velas de cuatro horas, pero el marco temporal puede
cambiarse mediante un parámetro.

No se incluye lógica de stop-loss ni take-profit; las posiciones se revierten únicamente cuando aparece
un patrón opuesto. La estrategia también dibuja velas, el indicador AO y las operaciones ejecutadas en
un gráfico para análisis visual.
