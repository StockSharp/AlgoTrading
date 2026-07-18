# Estrategia de Vela Color XMACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una implementación en StockSharp del asesor experto "ColorXMACDCandle". Opera utilizando el indicador MACD e interpreta los cambios de color del histograma o su línea de señal como señales de entrada.

## Idea

La estrategia analiza la pendiente de un componente MACD:

- **Modo Histogram** – Una nueva barra del histograma que sube por encima de la barra anterior señala un impulso alcista creciente. Una nueva barra que cae por debajo de la anterior señala impulso bajista.
- **Modo Signal line** – En su lugar se utiliza la pendiente de la línea de señal MACD. Una pendiente ascendente actúa como señal de compra, mientras que una descendente actúa como señal de venta.

Cuando el componente elegido gira hacia arriba y no estaba subiendo antes, cualquier posición corta puede cerrarse y puede abrirse una nueva posición larga. Cuando el componente gira hacia abajo y no estaba cayendo antes, cualquier posición larga puede cerrarse y puede abrirse una posición corta.

El comportamiento de apertura y cierre de posiciones está controlado por parámetros separados, permitiendo al usuario habilitar o deshabilitar cada acción de forma independiente.

## Parámetros

- `Mode` – Fuente de señales: `Histogram` o `SignalLine`.
- `FastPeriod` – Período de la EMA rápida para MACD.
- `SlowPeriod` – Período de la EMA lenta para MACD.
- `SignalPeriod` – Período de suavizado de la señal MACD.
- `EnableBuyOpen` – Permitir abrir posiciones largas.
- `EnableSellOpen` – Permitir abrir posiciones cortas.
- `EnableBuyClose` – Permitir cerrar posiciones largas.
- `EnableSellClose` – Permitir cerrar posiciones cortas.
- `CandleType` – Tipo de vela para los cálculos.

## Reglas de trading

1. Suscribirse a la serie de velas seleccionada y calcular el indicador MACD.
2. Rastrear la pendiente del histograma o la línea de señal según el modo seleccionado.
3. Cuando la pendiente gira hacia arriba, cerrar cualquier posición corta (si está permitido) y opcionalmente abrir una posición larga.
4. Cuando la pendiente gira hacia abajo, cerrar cualquier posición larga (si está permitido) y opcionalmente abrir una posición corta.

La estrategia no incluye mecanismos de stop-loss ni take-profit. La gestión del riesgo puede añadirse por separado si es necesario.
