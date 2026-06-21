# Estrategia RK's Framework Auto Color Gradient
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina Bollinger Bands %B y RSI en un único oscilador, lo mapea a un gradiente de color y opera cuando cruza la línea central.

## Lógica
- Calcula Bollinger Bands %B y el Índice de Fuerza Relativa.
- Normaliza ambos con un proceso estocástico y los promedia.
- Convierte el resultado en un gradiente de color seleccionable.
- Compra cuando el valor promediado está por encima de cero.
- Vende cuando el valor promediado está por debajo de cero.
