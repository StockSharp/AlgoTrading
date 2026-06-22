# Estrategia Renko Line Break vs RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el experto MetaTrader "RenkoLineBreak vs RSI" usando la API de alto nivel de StockSharp. Combina la detección de tendencia Renko con un filtro de retroceso RSI y ejecuta operaciones a través de órdenes stop pendientes ubicadas alrededor de una estructura de precio de tres velas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La tendencia Renko permanece alcista y el RSI cae hasta `50 - RsiShift` o por debajo. Se coloca una orden stop de compra en el máximo de la vela de tres barras atrás más `IndentFromHighLow`.
  - **Corto**: La tendencia Renko permanece bajista y el RSI sube hasta `50 + RsiShift` o por encima. Se coloca una orden stop de venta en el mínimo de la vela de tres barras atrás menos `IndentFromHighLow`.
  - Las órdenes pendientes se cancelan cuando la tendencia Renko cambia de dirección (`ToUp` / `ToDown`).
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Salidas de mercado cuando aparece la transición Renko opuesta (`ToDown` para largos, `ToUp` para cortos).
  - El RSI cruza de vuelta a través del punto medio (`50 ± RsiShift`).
  - Los rangos de velas alcanzando los niveles de stop-loss o take-profit planificados.
- **Stops**:
  - El stop-loss está anclado al extremo de las últimas tres velas más `IndentFromHighLow`.
  - El take-profit está a `TakeProfit` unidades de precio desde la entrada prevista (opcional cuando se establece en cero).
- **Valores predeterminados**:
  - `BoxSize` = 500m.
  - `RsiPeriod` = 4.
  - `RsiShift` = 20m.
  - `TakeProfit` = 1000m.
  - `IndentFromHighLow` = 50m.
  - `Volume` = 1m.
  - `CandleType` = marco temporal de 5 minutos.
- **Filtros**:
  - Categoría: Seguimiento de tendencia.
  - Dirección: Ambos.
  - Indicadores: Renko, RSI.
  - Stops: Stop fijo y take profit.
  - Complejidad: Intermedio.
  - Marco temporal: Híbrido (Renko + velas temporales).
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.

## Cómo funciona

1. Una suscripción Renko (`RenkoCandleMessage`) estima la dirección de la tendencia. Cuando un ladrillo Renko cambia de dirección, el estado de tendencia se establece en `ToUp` o `ToDown` por una barra para imitar el comportamiento del indicador original.
2. Simultáneamente, un flujo de velas basado en tiempo alimenta el indicador RSI y proporciona los últimos tres máximos/mínimos usados para los niveles de ruptura.
3. Cuando ambas condiciones de tendencia Renko y RSI se alinean, la estrategia registra una orden stop (compra o venta). Los niveles planificados de stop-loss y take-profit se almacenan y monitorean después de que se dispara la orden.
4. Tras la ejecución de la orden, los niveles de protección almacenados se activan. Las velas posteriores verifican si el precio alcanza los rangos de stop o objetivo; si es así, la posición se cierra a mercado.
5. Si el impulso se desvanece (RSI cruza de vuelta a través del punto medio) o la tendencia Renko cambia, la posición se cierra anticipadamente.

## Indicadores utilizados

- **Ladrillos Renko** para inferir el sesgo direccional y detectar transiciones entre estados alcistas y bajistas.
- **Relative Strength Index (RSI)** para calificar entradas exigiendo retrocesos contra la tendencia.

## Notas adicionales

- `IndentFromHighLow` modela el buffer del experto original que mantiene las órdenes de entrada y stop alejadas de los máximos y mínimos recientes.
- `TakeProfit` puede establecerse en cero para deshabilitar el objetivo de ganancia mientras deja la lógica de stop-loss intacta.
- La estrategia mantiene solo una orden pendiente a la vez y la cancela automáticamente cuando las condiciones del mercado invalidan la configuración.
