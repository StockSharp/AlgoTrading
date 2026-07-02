# Estrategia del comerciante Doji
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia convierte el asesor experto MQL4 "DojiTrader" en una muestra de StockSharp C#. Busca velas doji recientes y negocia una ruptura del rango doji durante las principales sesiones europeas y estadounidenses.

## Lógica comercial
- La estrategia procesa solo velas terminadas del período de tiempo seleccionado (velas de 30 minutos por defecto).
- Sólo se permite operar entre las 08:00 y las 17:00, hora de la plataforma.
- Aunque está plano, mira hacia atrás hasta tres velas completadas y recuerda el doji más reciente (el precio de apertura es igual al precio de cierre).
- Cuando la vela que sigue inmediatamente al doji se cierra por encima del máximo del doji, se arma una ruptura larga. Si cierra por debajo del mínimo doji, se arma una breve ruptura.
- Tan pronto como una vela posterior cierra más allá del precio de armado, la estrategia envía una orden de mercado en la dirección de ruptura.
- Después de la entrada, el rango del doji se conserva para el control de salida. La posición se cierra cuando:
  - La vela anterior vuelve a cerrar dentro del rango (larga: cierra por debajo del mínimo doji, corta: cierra por encima del máximo doji).
  - Los extremos de las velas alcanzan niveles de stop loss sintéticos o toma de ganancias que imitan las salidas de punto fijo originales MQL4.

## Parámetros
- **Volumen de orden** – volumen utilizado para órdenes de mercado.
- **Take de ganancias (pasos)** – distancia al objetivo de ganancias medida en pasos de precio.
- **Stop loss (pasos)** – distancia hasta el stop de protección en pasos de precio.
- **Tipo de vela**: período de tiempo de las velas utilizadas para la detección de señales.

Los cálculos de stop-loss y take-profit se basan en el paso del precio del valor, emulando el EA original que utilizaba distancias de pips fijas. Cuando no hay ningún doji válido dentro de las últimas tres velas, el estado de ruptura se borra y se reinicia la búsqueda.
