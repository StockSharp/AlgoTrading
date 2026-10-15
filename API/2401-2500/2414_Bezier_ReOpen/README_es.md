# Estrategia Bezier ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Bezier ReOpen** aplica un indicador personalizado de curva Bezier para seguir la dirección de la tendencia.
Cuando el indicador gira hacia arriba y el último valor está por encima del anterior, la estrategia puede abrir una posición larga.
Cuando el indicador gira hacia abajo, puede abrir una posición corta. Las posiciones existentes se cierran cuando el indicador cambia de dirección.
Tras entrar, se vuelven a abrir posiciones adicionales cada vez que el precio avanza un paso definido por el usuario, permitiendo escalar en la tendencia.

Esta implementación se basa en el Asesor Experto de MetaTrader `Exp_Bezier_ReOpen.mq5` (ID 16883).

## Detalles

- **Indicador**: Curva Bezier construida a partir de los últimos `BPeriod` precios y el parámetro `T` que define la tensión de la curva.
- **Entrada**:
  - **Largo**: la pendiente del indicador gira hacia arriba y el valor actual está por encima del valor anterior.
  - **Corto**: la pendiente del indicador gira hacia abajo y el valor actual está por debajo del valor anterior.
- **Salida**:
  - **Largo**: la pendiente del indicador gira hacia abajo.
  - **Corto**: la pendiente del indicador gira hacia arriba.
- **Re-entrada**: tras la entrada inicial, se envía una orden extra cada vez que el precio se mueve `PriceStep` desde el último precio de entrada, hasta `PosTotal` órdenes.
- **Stops**: stop-loss y take-profit opcionales definidos en unidades absolutas de precio.

## Parámetros

- `CandleType` – marco temporal de velas para cálculos. Por defecto: 4 horas.
- `BPeriod` – número de barras para el cálculo Bezier. Por defecto: 8.
- `T` – tensión de la curva Bezier (0..1). Por defecto: 0.5.
- `PriceType` – fuente de precio para el indicador (close, open, high, low, median, typical, weighted). Por defecto: weighted.
- `PriceStep` – distancia de precio para enviar órdenes adicionales. Por defecto: 300.
- `PosTotal` – número máximo de posiciones en la secuencia de escalado. Por defecto: 10.
- `BuyPosOpen` – permitir abrir posiciones largas. Por defecto: true.
- `SellPosOpen` – permitir abrir posiciones cortas. Por defecto: true.
- `BuyPosClose` – permitir cerrar largos en señal opuesta. Por defecto: true.
- `SellPosClose` – permitir cerrar cortos en señal opuesta. Por defecto: true.
- `StopLoss` – stop-loss en unidades de precio. Por defecto: 1000.
- `TakeProfit` – take-profit en unidades de precio. Por defecto: 2000.

## Etiquetas de Filtro
- Categoría: Seguimiento de tendencia
- Dirección: Ambos
- Indicadores: Personalizado
- Stops: Opcional
- Complejidad: Moderado
- Marco temporal: Medio plazo
- Nivel de riesgo: Moderado
