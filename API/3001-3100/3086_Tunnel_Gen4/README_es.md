# Estrategia de Cuadrícula con Cobertura Tunnel Gen4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica la lógica del experto MetaTrader "Tunnel gen4" usando la API de alto nivel de StockSharp. Mantiene una cobertura neutral al mercado abriendo un par inicial de compra/venta, duplica la posición en la dirección de la ruptura una vez que el precio recorre un número configurable de pips, y sale de toda la cesta cuando la misma distancia se cubre nuevamente más allá del segundo ancla.

## Lógica de Trading

- **Cobertura inicial:** Tan pronto como no existe exposición, la estrategia envía órdenes simultáneas de compra y venta a mercado con volumen `StartVolume`. La primera ejecución define el precio de referencia para todas las decisiones posteriores.
- **Detección de paso:** El `StepPips` configurado se convierte en un desplazamiento de precio usando el tamaño de tick del instrumento (con ajustes automáticos para citas forex de tres y cinco decimales). Las actualizaciones del mejor bid/ask del flujo Level 1 se comparan contra este desplazamiento.
- **Orden de refuerzo:** Cuando el mejor bid sube al menos un paso desde la primera ejecución, se envía una orden de venta con el doble del volumen base. Cuando el mejor ask baja al menos un paso, se emite una orden de compra del mismo tamaño. La primera ejecución de esta orden se convierte en el segundo ancla.
- **Terminación del ciclo:** Después de que el segundo ancla está activo, cualquier movimiento adicional del tamaño de un paso en cualquier dirección activa la liquidación completa de todas las posiciones abiertas. Una vez que ambos lados están cerrados, el estado se reinicia y puede comenzar un nuevo ciclo.
- **Validación de volumen:** El inicio de la estrategia verifica que tanto los volúmenes inicial como duplicado respeten los requisitos mínimos, máximos e incrementales del instrumento, para que cada orden enviada al conector sea ejecutable.

## Condiciones de Entrada

### Refuerzo largo
- Hay al menos una posición abierta del cobertura inicial.
- El segundo ancla aún no ha sido creado.
- El precio actual del mejor ask es menor o igual a `first_fill_price - StepPips_en_precio`.

### Refuerzo corto
- Hay al menos una posición abierta de la cobertura inicial.
- El segundo ancla aún no ha sido creado.
- El precio actual del mejor bid es mayor o igual a `first_fill_price + StepPips_en_precio`.

## Gestión de Salida

- **Cierre de cesta:** Una vez que el segundo ancla está definido, si el mejor bid sube por encima de `second_anchor + StepOffset` o el mejor ask cae por debajo de `second_anchor - StepOffset`, se envían órdenes a mercado para cerrar la exposición larga y corta acumulada. Las órdenes de cierre se rastrean para garantizar que el estado se reinicie solo después de que todas las operaciones sean confirmadas.
- **Reinicio de estado:** Después de que ambos lados estén cerrados y no queden órdenes de cierre activas, la estrategia limpia los anclas internos y espera a que se abra una nueva cobertura.

## Datos e Indicadores

- La suscripción Level 1 entrega los mejores precios bid y ask usados para las comparaciones de paso.
- No se requieren indicadores adicionales; toda la lógica funciona con actualizaciones de cotización en bruto.
- La conversión del paso de precio imita el ajuste punto-a-pip de MetaTrader, por lo que los símbolos forex con tres o cinco decimales se comportan igual que en el experto origen.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `StartVolume` | Volumen de las órdenes de compra y venta que forman la cobertura inicial. |
| `StepPips` | Distancia en pips que activa la orden de refuerzo y la posterior salida de la cesta. |

## Notas de Implementación

- StockSharp mantiene una posición neta por instrumento. La estrategia mantiene contadores de exposición internos para emular los tickets largos y cortos separados usados por el experto MetaTrader y emite órdenes a mercado con los volúmenes acumulados al cerrar la cesta.
- Como la lógica depende de spreads en tiempo real, proporcione datos Level 1 tanto en backtests como en sesiones de trading en vivo. La falta de información bid/ask deshabilita el bucle de trading.
- Asegúrese de que la cuenta de trading admita órdenes de compra y venta simultáneas para el mismo instrumento, ya que el algoritmo asume que ambos lados de la cobertura pueden coexistir hasta que se cumpla la condición de salida.
