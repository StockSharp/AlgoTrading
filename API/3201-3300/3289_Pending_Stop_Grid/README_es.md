# Estrategia de cuadrícula de stop pendientes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **estrategia de cuadrícula de stop pendientes** es una conversión directa del asesor experto de MetaTrader 4 `new.mq4`. La estrategia mantiene dos escaleras simétricas de órdenes pendientes:

- Una secuencia de órdenes buy stop por encima del precio ask actual.
- Una secuencia de órdenes sell stop por debajo del precio bid actual.

Cada nivel adicional aumenta tanto la distancia de la orden como el volumen operado de forma proporcional a su posición dentro de la escalera. Los objetivos de stop-loss y take-profit se asignan individualmente a cada orden.

## Lógica de trading
1. La estrategia se suscribe a datos Level 1 y rastrea continuamente los últimos mejores precios bid y ask.
2. Cuando hay datos de mercado y permisos de trading disponibles, calcula el tamaño de pip usando el paso de precio del valor (normalizando automáticamente símbolos de cinco y tres dígitos a valores pip estándar).
3. Antes de colocar órdenes, la estrategia valida que el volumen base configurado respete las restricciones de volumen mínimo y máximo del instrumento.
4. Para cada índice `i` de 1 a `NumberOfTrades`:
   - El volumen de la orden se calcula como `BaseVolume * i` y se redondea al paso permitido más cercano.
   - Se coloca un buy stop en `Ask + DistancePips * i * pipSize` con desplazamientos opcionales de stop-loss y take-profit.
   - Se coloca un sell stop en `Bid - DistancePips * i * pipSize` con desplazamientos reflejados de stop-loss y take-profit.
5. Si una orden se ejecuta, cancela o rechaza, el espacio correspondiente en la escalera se limpia y se repone inmediatamente con una nueva orden pendiente cuando los datos de mercado lo permiten.
6. `StartProtection()` incorporado se llama al iniciar para activar los controles de riesgo de la plataforma.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `BaseVolume` | Volumen de la primera orden pendiente. Cada orden posterior multiplica esta base por su índice. | `0.1` |
| `NumberOfTrades` | Número de órdenes buy stop y sell stop mantenidas simultáneamente. | `10` |
| `DistancePips` | Distancia (en pips) entre el precio de mercado y cada nivel de orden pendiente. | `10` |
| `StopLossPips` | Distancia de stop-loss asignada a cada orden. Establecer en cero para desactivar la colocación de stop-loss. | `10` |
| `TakeProfitPips` | Distancia de take-profit asignada a cada orden. Establecer en cero para desactivar la colocación de take-profit. | `10` |

Todos los parámetros se exponen como parámetros de estrategia optimizables y se validan para evitar valores negativos o cero (donde corresponda).

## Notas adicionales
- Los volúmenes se redondean al paso permitido más cercano y se limitan dentro de los límites mínimo y máximo definidos por la bolsa.
- Los precios se normalizan con `Security.ShrinkPrice` para respetar el tamaño de tick del instrumento.
- La estrategia no conserva estado histórico: reconstruye toda la escalera cuando se reinicia el valor o cambian los permisos de trading.
- La lógica evita buffers manuales de indicadores en favor de las vinculaciones de la API de alto nivel de StockSharp, siguiendo las directrices de conversión del proyecto.
