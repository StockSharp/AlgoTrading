# Estrategia de avalancha
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia Avalanche es un sistema de reversión a la media estilo cuadrícula inspirado en el asesor experto original MetaTrader Avalanche v1.2. La idea es monitorear la relación entre el precio y un precio de referencia de equilibrio (ERP) de marco temporal más alto calculado como un promedio móvil simple. Cuando el precio cotiza por debajo del ERP, la estrategia espera un rebote hacia el promedio y acumula posiciones largas. Cuando el precio cotiza por encima del ERP, la estrategia busca una caída y acumula posiciones cortas. Cada posición adicional está espaciada por umbrales de distancia configurables, mientras que cada entrada recibe niveles individuales de stop-loss y take-profit.

Este puerto StockSharp se centra en el tramo "hacia" del algoritmo original. Las órdenes de cobertura fuera del ERP de la versión MQL no se replican porque las estrategias StockSharp operan en una sola posición neta, pero la lógica de acumulación, almacenamiento en búfer y toma de ganancias de la red se mantiene fiel al enfoque original.

## como funciona

1. Suscríbase a dos series de velas: el marco temporal de operaciones y un marco temporal de ERP que alimenta la media móvil.
2. Calcule una media móvil simple de ERP y determine si el precio está posicionado por encima o por debajo de ella. Un búfer configurable evita cambios frecuentes.
3. Cuando aparezca un nuevo sesgo de ERP, cierre cualquier cuadrícula abierta y espere nuevas señales.
4. Abra una posición inicial en la dirección que debería hacer que el precio regrese hacia el ERP (largo por debajo, corto por arriba) si la bandera `OpenStartingOrders` está habilitada.
5. Continúe agregando posiciones en la misma dirección cuando el precio avance una distancia `IntervalToward` (apilamiento de impulso).
6. Agregue entradas de protección adicionales cuando el precio se mueva contra la cuadrícula en `IntervalToward + StackBufferToward` (apilamiento de martingala).
7. Cada entrada tiene su propio objetivo de stop-loss y take-profit medido en puntos, lo que garantiza que las partes rentables se puedan cerrar individualmente mientras la red continúa gestionando la exposición restante.

## Parámetros

| Nombre | Descripción |
| --- | --- |
| `BaseVolume` | Volumen de orden base utilizado antes de aplicar multiplicadores. |
| `TowardMultiplier` | Multiplicador de lote para entradas estándar hacia ERP. |
| `TowardInterestMultiplier` | Multiplicador utilizado cuando el instrumento paga un swap positivo en la dirección de negociación. |
| `IntervalToward` | Distancia en puntos requerida para agregar una pila de seguimiento de tendencias. |
| `StackBufferToward` | Se agrega un colchón adicional al intervalo cuando se acumula contra movimientos adversos de precios. |
| `TakeProfitToward` | Distancia de toma de ganancias en puntos para cada entrada. Establezca en `0` para desactivar. |
| `StopLossToward` | Distancia de stop-loss en puntos para cada entrada. Establezca en `0` para desactivar. |
| `ErpPeriod` | Número de periodos para la media móvil simple del ERP. |
| `ErpChangeBuffer` | Búfer (en puntos) aplicado alrededor del ERP antes de cambiar el sesgo. |
| `CandleType` | Plazo de negociación utilizado para activar entradas y salidas. |
| `ErpCandleType` | Plazo utilizado para calcular la media móvil del ERP. |
| `OpenStartingOrders` | Si está habilitado, abre inmediatamente la primera orden de la cuadrícula cuando se cumplen las condiciones. |

## Diferencias vs. el EA original

- Solo se implementa el tramo hacia ERP porque la estrategia StockSharp mantiene una única posición neta. Se omiten las órdenes de cobertura.
- La ejecución de órdenes se basa en órdenes de mercado en lugar de las órdenes stop pendientes utilizadas por la versión MQL.
- La detección de la dirección del swap se conserva para elegir entre los multiplicadores estándar y de interés.

## Consejos de uso

- Ajuste `IntervalToward` y `StackBufferToward` para controlar la agresividad con la que la cuadrícula agrega nuevas operaciones.
- Garantizar que el instrumento y los plazos seleccionados proporcionen suficiente liquidez; Los sistemas de red pueden acumular una exposición considerable.
- Combine la estrategia con controles de riesgo externos (paradas de acciones, filtros de sesión) cuando se ejecute en producción.
