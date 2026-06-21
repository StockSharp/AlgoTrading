# Estrategia de Oscilador Ichimoku
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Ichimoku Oscillator** utiliza un oscilador personalizado derivado del indicador Ichimoku. El oscilador se define como la diferencia entre la línea de retraso y Senkou Span B menos la diferencia entre Tenkan-sen y Kijun-sen. El valor resultante se suaviza con una media móvil Jurik.

La estrategia entra en posiciones cuando este oscilador suavizado cambia de dirección y cruza su valor anterior, intentando capturar tendencias emergentes.

## Cómo funciona
- **Entrada Largo**: El oscilador sube y el valor actual cruza por encima del valor anterior. Cualquier posición corta se cierra antes de abrir la larga.
- **Entrada Corto**: El oscilador baja y el valor actual cruza por debajo del valor anterior. Cualquier posición larga se cierra antes de abrir la corta.
- Se aplican stop loss y take profit opcionales en porcentaje para la gestión del riesgo.

## Parámetros
- **Tenkan Period** – Período Tenkan-sen del indicador Ichimoku.
- **Kijun Period** – Período Kijun-sen del indicador Ichimoku.
- **Senkou Span B Period** – Período Senkou Span B del indicador Ichimoku.
- **Smoothing Period** – Período para el suavizado con la media móvil Jurik del oscilador.
- **Candle Type** – Marco temporal utilizado para los cálculos.
- **Stop Loss %** – Stop loss expresado en porcentaje.
- **Enable Stop Loss** – Activa o desactiva la protección de stop loss.
- **Take Profit %** – Take profit expresado en porcentaje.

## Indicadores
- Ichimoku
- Jurik Moving Average

## Notas
Esta estrategia está destinada a fines educativos y debe probarse en datos históricos antes de operar en tiempo real.
