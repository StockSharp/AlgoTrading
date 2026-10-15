# Estrategia Binary Wave
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Binary Wave combina varios indicadores técnicos clásicos en una única "ola" binaria. Cada indicador contribuye con +1 o -1 dependiendo de su estado alcista o bajista. La suma ponderada de todas las señales forma la ola final utilizada para las decisiones de trading.

## Parámetros

- **Mode** – algoritmo de entrada: `Breakdown` reacciona al cruce cero; `Twist` reacciona a los cambios de dirección de la ola.
- **Candle Type** – marco temporal de las velas para todos los cálculos.
- **Indicator Periods** – longitudes para MA, MACD (rápido, lento, señal), CCI, Momentum, RSI y ADX.
- **Weights** – contribución de cada indicador a la ola. Establecer un peso en 0 deshabilita el indicador.
- **Trading Permissions** – habilitar o deshabilitar entradas y salidas largas/cortas por separado.
- **Risk** – stop-loss y take-profit en porcentaje del precio de entrada.

## Cómo funciona

1. Suscribirse a la serie de velas especificada y calcular todos los indicadores.
2. Para cada vela completada, evaluar el estado de cada indicador y convertirlo a un valor binario (+1 / -1).
3. Sumar los valores ponderados para obtener la ola actual.
4. Generar señales de trading:
   - **Breakdown**: entrar largo cuando la ola cruza por encima de cero, entrar corto cuando cruza por debajo de cero.
   - **Twist**: entrar largo cuando la ola cambia de dirección hacia arriba, entrar corto cuando gira hacia abajo.
5. El stop-loss y take-profit de protección opcionales son gestionados por la protección de posición integrada.

Este enfoque permite combinar flexiblemente múltiples indicadores manteniendo la lógica de trading sencilla.
