# Estrategia Órdenes Pendientes Por Tiempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el clásico experto de MetaTrader "Pending orders by time" para StockSharp. Funciona con un horario discreto: cada día coloca órdenes stop simétricas alrededor del mercado cuando comienza una nueva hora de sesión, y elimina todas las órdenes más las posiciones abiertas a una hora de cierre especificada. La implementación mantiene las entradas originales basadas en pips, las convierte a unidades de precio nativas y usa el API de alto nivel para gestionar el riesgo.

## Cómo funciona

1. **Disparador basado en tiempo** – Cuando se recibe una vela que termina a la hora de apertura configurada, la estrategia envía un buy stop por encima del ask y un sell stop por debajo del bid. Ambas órdenes se desplazan por el parámetro `Distance (pips)` convertido a unidades de precio.
2. **Órdenes protectoras** – `StartProtection` adjunta automáticamente protección de stop-loss y take-profit usando las distancias en pips definidas en los parámetros. `ManageRisk` también actúa como salvaguarda, cerrando cualquier posición residual si una vela completada muestra que se han cruzado los umbrales.
3. **Cierre de sesión** – Cuando llega la hora de cierre, la estrategia cancela cualquier orden pendiente restante y sale forzosamente de las operaciones abiertas independientemente del beneficio o pérdida. Esto reproduce el comportamiento del experto original de restablecer al final de la sesión.
4. **Tamaño de pip con conciencia de dígitos** – El multiplicador de pip emula la implementación de MetaTrader multiplicando el paso de precio por diez para símbolos cotizados con tres o cinco decimales (p. ej., JPY o pares FX de 5 dígitos). Esto mantiene las entradas heredadas consistentes entre brokers.

El tipo de vela predeterminado son barras de 30 minutos para mantenerse bajo la restricción original de períodos menores que H1. Se puede usar cualquier otro marco temporal, siempre que las marcas de tiempo horarias resultantes coincidan con las horas de sesión deseadas.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Opening Hour` | Hora (0-23) cuando la estrategia colocará el par de órdenes stop. | 9 |
| `Closing Hour` | Hora (0-23) cuando se cancelan todas las órdenes y se cierran las posiciones. | 2 |
| `Distance (pips)` | Desplazamiento, en pips, entre el precio actual y las entradas stop pendientes. | 20 |
| `Stop Loss (pips)` | Distancia en pips para el stop protector una vez que hay una posición abierta. | 20 |
| `Take Profit (pips)` | Distancia en pips para el objetivo de beneficio una vez que hay una posición abierta. | 500 |
| `Order Volume` | Cantidad usada al colocar cada orden stop pendiente. | 0.1 |
| `Candle Type` | Marco temporal que impulsa el horario. | Marco temporal de 30 minutos |

Todos los parámetros pueden optimizarse. Las entradas basadas en pips se convierten internamente usando el paso de precio del instrumento para que permanezcan portátiles entre símbolos FX con diferente precisión decimal.

## Flujo de trabajo diario

1. **En cada cierre de vela** la estrategia verifica si se ha alcanzado la distancia de stop-loss o take-profit. Si es así, cierra la posición activa a mercado.
2. **Cuando se alcanza la hora de cierre** cancela cualquier orden pendiente no llenada y sale de la posición, asegurando que el libro esté plano antes de la próxima sesión.
3. **Cuando se alcanza la hora de apertura** (y la estrategia está plana) cancela órdenes antiguas por precaución y envía un nuevo sell stop por debajo del bid y un buy stop por encima del ask. Las órdenes están reflejadas alrededor del spread para que se pueda capturar cualquier ruptura.
4. **A lo largo de la sesión** la protección a nivel de plataforma creada por `StartProtection` mantiene un stop-loss y take-profit adjuntos, actuando inmediatamente si la acción del precio dentro de la barra alcanza los umbrales.

## Notas de uso

- Use instrumentos cuyo tamaño de tick represente un "punto" único para que el ajuste de pip refleje el experto original. Los tamaños de tick exóticos pueden requerir ajuste manual de los parámetros de distancia.
- La lógica asume un ciclo de trading por día. Si usa datos intradía con múltiples coincidencias de apertura/cierre, ajuste las horas en consecuencia.
- Dado que todas las acciones ocurren al completar la vela, seleccione un tamaño de vela que coincida con la frecuencia con que desea evaluar el horario. Por ejemplo, velas horarias proporcionan la misma cadencia que la versión de MetaTrader.
- La estrategia solo coloca nuevas órdenes pendientes cuando la posición está plana, evitando sobreexposición si una operación de ruptura todavía está activa durante la próxima hora de apertura.

## Diferencias con la versión MQL

- Las salidas protectoras se manejan a través de `StartProtection` más verificaciones explícitas, aprovechando el API de alto nivel de StockSharp en lugar de la asignación directa de stop-loss en el ticket de la orden pendiente.
- Los precios bid/ask se leen de `Security.BestBid` y `Security.BestAsk`. Si esas cotizaciones no están disponibles, el cierre de la vela se usa como referencia de respaldo.
- Se usan órdenes de mercado para liquidar posiciones a la hora de cierre por simplicidad y para evitar comportamientos específicos del broker.
