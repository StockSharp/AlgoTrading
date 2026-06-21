# Estrategia Straddle News
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia diseñada para publicaciones de noticias de alta volatilidad. Coloca órdenes stop simétricas en ambos lados del precio actual para capturar rupturas. Una vez que se activa una orden, la orden pendiente opuesta se cancela y un trailing stop protege la posición abierta.

## Detalles

- **Criterios de entrada**: esperar spread por debajo de `SpreadOperation`, luego colocar compra stop en Ask + `PipsAway` puntos y venta stop en Bid - `PipsAway` puntos
- **Largo/Corto**: Ambos
- **Criterios de salida**: stop loss o take profit de protección, o trailing stop cuando el precio retrocede `TrailingStop` puntos
- **Stops**: Stop loss y take profit iniciales mediante `StartProtection`; trailing stop personalizado en código
- **Valores predeterminados**:
  - `StopLoss` = 100
  - `TakeProfit` = 300
  - `TrailingStop` = 50
  - `PipsAway` = 50
  - `BalanceUsed` = 0.01
  - `SpreadOperation` = 25
  - `Leverage` = 400
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Level1 / Tick
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Alto

## Cómo funciona

1. Suscribirse a cotizaciones Level1 para acceder a los precios de compra y venta actuales.
2. Cuando el spread es suficientemente pequeño, calcular el volumen usando el valor de la cartera, el apalancamiento y `BalanceUsed`.
3. Colocar órdenes pendientes de compra y venta stop con los desplazamientos definidos por `PipsAway`.
4. Cuando se abre una posición, cancelar la orden pendiente opuesta.
5. Adjuntar órdenes de stop loss y take profit basadas en `StopLoss` y `TakeProfit`.
6. Rastrear el precio más alto/más bajo desde la entrada y salir si el precio retrocede más de `TrailingStop` puntos.
