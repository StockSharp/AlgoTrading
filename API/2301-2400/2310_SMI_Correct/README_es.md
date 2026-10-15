# Estrategia SMI Correct
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia SMI Correct implementa un sistema de trading basado en el indicador Stochastic Momentum Index (SMI). La estrategia observa la línea SMI y su línea de señal de media móvil. Se abre una posición larga cuando el SMI cruza por debajo de la línea de señal. Se abre una posición corta cuando el SMI cruza por encima de la línea de señal.

## Parámetros
- **Candle Type** – marco temporal de las velas utilizadas para los cálculos.
- **SMI Length** – número de períodos para el cálculo del Estocástico.
- **Signal Length** – período de suavizado para la línea de señal.

## Cómo funciona
1. La estrategia se suscribe a las velas del tipo especificado.
2. Para cada vela completada, actualiza el oscilador Estocástico y la media móvil de señal.
3. Cuando el SMI cruza por debajo de la línea de señal, se cierra cualquier posición corta y se abre una posición larga.
4. Cuando el SMI cruza por encima de la línea de señal, se cierra cualquier posición larga y se abre una posición corta.

El ejemplo también dibuja las velas y las líneas del indicador en un gráfico para su visualización.
