# Estrategia Color Coppock
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Color Coppock Strategy** implementa un sistema de trading basado en un oscilador Coppock modificado. El oscilador suma dos valores de Rate of Change (ROC) y suaviza el resultado con una media móvil. El momentum ascendente genera señales largas, mientras que el momentum descendente genera señales cortas.

## Cómo Funciona

1. Calcular dos valores ROC con diferentes períodos.
2. Sumar ambos valores ROC y aplicar una Media Móvil Simple para el suavizado.
3. Comparar el valor actual del oscilador con los dos valores anteriores:
   - Si el oscilador gira hacia arriba después de descender, la estrategia entra en una posición larga o cierra la posición corta existente.
   - Si el oscilador gira hacia abajo después de subir, la estrategia entra en una posición corta o cierra la posición larga existente.
4. El volumen de la posición se toma de la propiedad `Volume` de la estrategia.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `Roc1Period` | Período para el primer cálculo ROC. |
| `Roc2Period` | Período para el segundo cálculo ROC. |
| `SmoothingPeriod` | Período SMA aplicado a la suma de ambos valores ROC. |
| `CandleType` | Tipo de vela utilizado para los cálculos del indicador. |

## Uso

1. Adjuntar la estrategia a un valor y establecer los parámetros deseados.
2. La estrategia se suscribe a las velas especificadas y procesa solo las velas finalizadas.
3. Las operaciones se ejecutan con órdenes de mercado usando el volumen predeterminado.

## Notas

- La estrategia utiliza solo llamadas de API de alto nivel como `SubscribeCandles` y ayudantes de órdenes de mercado.
- Todos los comentarios dentro del código están escritos en inglés.
