# Estrategia Renko Line Break vs RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el experto MetaTrader "RenkoLineBreak vs RSI" usando la API de alto nivel de StockSharp. Combina la detección de tendencia Renko con un filtro de retroceso RSI y entra a mercado en cuanto una estructura de precio de tres velas confirma la configuración. Los ladrillos Renko se calculan dentro de la propia estrategia a partir de los cierres de las velas temporales, por lo que una única suscripción de velas alimenta todo.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La tendencia Renko permanece alcista y el RSI cae hasta `50 - RsiShift` o por debajo. La configuración se valida contra un nivel de referencia igual al máximo de la vela de tres barras atrás más `IndentFromHighLow`, y se envía una orden de compra a mercado al cierre de la vela de señal.
  - **Corto**: La tendencia Renko permanece bajista y el RSI sube hasta `50 + RsiShift` o por encima. La configuración se valida contra un nivel de referencia igual al mínimo de la vela de tres barras atrás menos `IndentFromHighLow`, y se envía una orden de venta a mercado al cierre de la vela de señal.
  - No se abre ninguna posición nueva mientras la tendencia Renko está en un estado de transición (`ToUp` / `ToDown`); la configuración almacenada se descarta.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Salidas de mercado cuando aparece la transición Renko opuesta (`ToDown` para largos, `ToUp` para cortos).
  - El RSI cruza de vuelta a través del punto medio (`50 ± RsiShift`).
  - Los rangos de velas alcanzando los niveles de stop-loss o take-profit planificados.
- **Stops**:
  - El stop-loss está anclado al extremo de las últimas tres velas más `IndentFromHighLow`.
  - El take-profit está a `TakeProfit` unidades de precio desde el nivel de ruptura de referencia (opcional cuando se establece en cero).
- **Valores predeterminados**:
  - `BoxSize` = 100m.
  - `RsiPeriod` = 4.
  - `RsiShift` = 10m.
  - `TakeProfit` = 1000m.
  - `IndentFromHighLow` = 50m.
  - `Volume` = 1m.
  - `CandleType` = marco temporal de 2 horas.
- **Filtros**:
  - Categoría: Seguimiento de tendencia.
  - Dirección: Ambos.
  - Indicadores: Renko, RSI.
  - Stops: Stop fijo y take profit.
  - Complejidad: Intermedio.
  - Marco temporal: Un solo marco temporal (los ladrillos Renko se derivan de los cierres de las velas).
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Medio.

## Cómo funciona

1. Los ladrillos Renko se construyen dentro de la estrategia a partir de los cierres de las velas temporales: un ladrillo que continúa la dirección actual se genera cuando el cierre se aleja un `BoxSize` completo del ancla actual, mientras que un ladrillo que invierte la dirección exige dos `BoxSize`. Antes de que el primer ladrillo fije una dirección, basta con un box en cualquier sentido. Se generan tantos ladrillos como abarque el movimiento y el ancla se desplaza con ellos. Cuando un ladrillo cambia de dirección, el estado de tendencia se establece en `ToUp` o `ToDown` por un paso para imitar el comportamiento del indicador original.
2. El mismo flujo de velas alimenta el indicador RSI y proporciona los últimos tres máximos/mínimos usados para los niveles de ruptura, por lo que la estrategia abre exactamente una suscripción de datos de mercado.
3. Cuando ambas condiciones de tendencia Renko y RSI se alinean, la estrategia envía una orden a mercado (compra o venta). Los niveles planificados de stop-loss y take-profit se almacenan y se monitorean una vez que la posición está abierta.
4. Una vez abierta la posición, los niveles de protección almacenados se activan. Las velas posteriores verifican si el precio alcanza los rangos de stop o objetivo; si es así, la posición se cierra a mercado.
5. Si el impulso se desvanece (RSI cruza de vuelta a través del punto medio) o la tendencia Renko cambia, la posición se cierra anticipadamente.

## Indicadores utilizados

- **Ladrillos Renko** derivados de los cierres de las velas temporales con el paso `BoxSize`, para inferir el sesgo direccional y detectar transiciones entre estados alcistas y bajistas.
- **Relative Strength Index (RSI)** para calificar entradas exigiendo retrocesos contra la tendencia.

## Notas adicionales

- `IndentFromHighLow` modela el buffer del experto original que mantiene el nivel de ruptura de referencia y el stop-loss alejados de los máximos y mínimos recientes.
- `TakeProfit` puede establecerse en cero para deshabilitar el objetivo de ganancia mientras deja la lógica de stop-loss intacta.
- La estrategia mantiene una sola posición a la vez: solo considera una nueva entrada cuando está fuera del mercado y descarta la configuración almacenada en cuanto las condiciones del mercado la invalidan.
