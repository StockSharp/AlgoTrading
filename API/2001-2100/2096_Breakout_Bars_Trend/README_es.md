# Estrategia de Ruptura de Barras de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia detecta reversiones de tendencia utilizando el indicador Parabolic SAR. Espera un número configurable de reversiones negativas antes de entrar en la nueva dirección de la tendencia. Las distancias para el stop-loss y el take-profit se miden en pips o como un porcentaje del precio de entrada.

## Parámetros

- **Reversal Mode** – elegir entre cálculos de distancia basados en pips o en porcentaje.
- **Delta** – movimiento mínimo de precio requerido entre reversiones.
- **Negative Signals** – cuántas reversiones fallidas deben ocurrir antes de abrir una operación.
- **Stop Loss** – distancia de protección de pérdidas desde el precio de entrada.
- **Take Profit** – distancia del objetivo de beneficio desde el precio de entrada.
- **Candle Type** – serie de velas utilizada para los cálculos del indicador.

## Lógica

1. Suscribirse a los datos de velas y calcular el Parabolic SAR.
2. Cuando el Parabolic SAR cambia de dirección y el precio se mueve al menos *Delta*, almacenar el precio de reversión.
3. Contar las reversiones negativas donde el precio se movió contra la tendencia anterior.
4. Una vez que el contador alcanza el valor de **Negative Signals**, abrir una posición en la nueva dirección de la tendencia.
5. Cada vela comprueba los niveles de stop-loss y take-profit usando el **Reversal Mode** seleccionado.
6. Las posiciones se cierran ante un cambio de tendencia opuesto o cuando se alcanzan los límites de riesgo.

La estrategia es adecuada para sistemas de ruptura de seguimiento de tendencia y puede optimizarse ajustando las distancias de delta, stop-loss y take-profit.
