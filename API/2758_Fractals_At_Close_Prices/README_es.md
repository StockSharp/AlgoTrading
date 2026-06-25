# Estrategia de Fractals en Precios de Cierre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port a StockSharp del asesor experto de MetaTrader 5 **"Fractals at Close prices"** de Vladimir Karputov. Analiza cinco precios de cierre consecutivos para detectar fractales al estilo de Bill Williams construidos estrictamente sobre cierres en lugar de máximos o mínimos. Los dos fractales alcistas y bajistas más recientes se comparan para determinar la tendencia activa. Cuando el último fractal alcista aparece por encima del anterior, la estrategia abre una posición larga. Cuando el último fractal bajista se forma por debajo del anterior, abre una posición corta. Las posiciones opuestas siempre se cierran antes de entrar en una nueva operación, por lo que la estrategia permanece en como máximo una dirección a la vez.

Las operaciones solo se permiten entre la hora de inicio y la hora de fin configurables. Si la hora actual cae fuera de esta ventana, todas las posiciones abiertas se cierran inmediatamente, replicando el comportamiento del EA original. El filtro de tiempo admite ventanas intradías (inicio < fin), sesiones nocturnas que cruzan la medianoche (inicio > fin) y trading durante todo el día (inicio == fin).

## Lógica del indicador
* Cada vela finalizada se añade a una cola deslizante de cinco elementos de precios de cierre.
* Una vez disponibles cinco valores, se evalúa el cierre intermedio (dos velas atrás):
  * Se registra un fractal alcista si el cierre intermedio es estrictamente mayor que los dos cierres más antiguos y mayor o igual a los dos cierres más nuevos.
  * Se registra un fractal bajista si el cierre intermedio es estrictamente menor que los dos cierres más antiguos y menor o igual a los dos cierres más nuevos.
* Los fractales alcistas y bajistas más recientes y anteriores se almacenan para comparación posterior.
* Se detecta una tendencia alcista cuando el último fractal alcista es mayor que el anterior. Se detecta una tendencia bajista cuando el último fractal bajista es menor que el anterior.

## Reglas de trading
1. **Entradas largas**
   * Cerrar cualquier posición corta activa a mercado.
   * Si no hay posición larga abierta, comprar `OrderVolume` a mercado en el cierre que confirmó la secuencia de fractal alcista.
2. **Entradas cortas**
   * Cerrar cualquier posición larga activa a mercado.
   * Si no hay posición corta abierta, vender `OrderVolume` a mercado cuando se confirma una secuencia de fractal bajista.
3. **Control de sesión**
   * Antes de aplicar señales, la estrategia verifica que `candle.OpenTime.Hour` esté dentro de la ventana de trading. Si no, se llama a `CloseAllPositions` y se ignora la barra.

## Gestión de riesgos
* Las distancias de stop-loss y take-profit se expresan en pips. La implementación reproduce el enfoque MT5: el punto del símbolo se multiplica por diez cuando el instrumento tiene 3 o 5 decimales. El valor del pip resultante se multiplica entonces por las distancias configuradas.
* Al entrar en una posición, los niveles iniciales de stop-loss y take-profit se almacenan internamente. Como StockSharp no gestiona automáticamente las órdenes protectoras al estilo MT5, la estrategia monitorea velas terminadas y sale a mercado cuando su rango de precios toca el nivel almacenado.
* Los stops de seguimiento siguen las reglas originales del EA. Se calcula un nuevo stop como `close ± TrailingStop` una vez que el beneficio supera `TrailingStop + TrailingStep`. El trailing stop solo avanza si el movimiento desde el stop anterior es al menos `TrailingStep`.
* Cuando termina el horario de trading, todas las posiciones se cierran independientemente del estado del trailing. Esto replica el EA llamando a `CloseAllPositions` fuera de la sesión permitida.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `OrderVolume` | Volumen utilizado para cada orden a mercado. | `0.1` |
| `StartHour` | Hora (0-23) en que el trading se activa. Si es igual a `EndHour`, la estrategia opera todo el día. | `10` |
| `EndHour` | Hora (0-23) en que el trading deja de aceptar nuevas señales. | `22` |
| `StopLossPips` | Distancia del stop-loss expresada en pips. `0` desactiva el stop. | `30` |
| `TakeProfitPips` | Distancia del take-profit expresada en pips. `0` desactiva el take. | `50` |
| `TrailingStopPips` | Distancia base del trailing stop en pips. `0` desactiva el trailing. | `15` |
| `TrailingStepPips` | Beneficio adicional (en pips) requerido antes de que se avance el trailing stop. | `5` |
| `CandleType` | Tipo de datos de velas suscrito por la estrategia. El predeterminado es velas de marco temporal de 1 hora. | `1 hour TimeFrame` |

## Notas de implementación
* La estrategia usa `SubscribeCandles` con la API de alto nivel y no registra indicadores manualmente, siguiendo las directrices del proyecto.
* Las salidas protectoras (stop, take-profit, trailing stop) se ejecutan enviando órdenes a mercado después de que una vela termina, porque StockSharp no gestiona automáticamente las órdenes protectoras de MT5.
* El filtrado de sesión, la detección de fractales y la lógica de trailing siguen estrictamente la estructura del EA, incluyendo el cierre de todas las posiciones cuando el filtro de hora no se cumple.
* La lógica de escalado de pips refleja la implementación MT5 multiplicando el punto del símbolo por diez en instrumentos de 3 o 5 decimales, asegurando distancias de precio equivalentes.

## Consejos de uso
1. Adjuntar la estrategia a un símbolo y establecer `OrderVolume` al tamaño de lote preferido.
2. Elegir un tipo de vela que coincida con el marco temporal usado en MetaTrader 5 (el EA original funciona en cualquier marco temporal).
3. Ajustar la ventana de trading a la sesión del bróker o las horas deseadas.
4. Ajustar las distancias basadas en pips para reflejar la volatilidad del instrumento. Un `TrailingStepPips` mayor reduce la frecuencia del trailing, mientras que valores menores hacen que el stop siga el precio más de cerca.
5. Monitorear los registros para entradas y salidas; la estrategia dibuja operaciones en el área de gráfico opcional para validación visual rápida.
