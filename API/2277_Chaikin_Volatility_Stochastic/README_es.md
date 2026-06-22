# Estrategia de Volatilidad Chaikin Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia aplica un oscilador estocástico a los valores de volatilidad de Chaikin para capturar reversiones de tendencia. El rango máximo-mínimo de cada vela se suaviza con una EMA, luego se normaliza con un cálculo estocástico y finalmente se suaviza con una media móvil ponderada.

Cuando el oscilador suavizado se vuelve descendente después de subir, se abre una posición larga y se cierra cualquier posición corta. Cuando el oscilador se vuelve ascendente después de caer, se abre una posición corta y se cierra cualquier posición larga.

## Parámetros
- **Candle Type**: marco temporal para la suscripción de velas.
- **EMA Length**: período de suavizado para el rango máximo-mínimo.
- **Stochastic Length**: período retrospectivo para el cálculo estocástico.
- **WMA Length**: período de media móvil ponderada para suavizar el oscilador.
- **Enable Longs / Enable Shorts**: alternar direcciones de operación permitidas.

## Indicadores
- ExponentialMovingAverage
- Highest y Lowest
- WeightedMovingAverage

## Reglas de Trading
- **Entrada larga**: el oscilador estaba subiendo y se vuelve descendente.
- **Entrada corta**: el oscilador estaba bajando y se vuelve ascendente.
- Las posiciones opuestas se cierran al cambiar la señal.
