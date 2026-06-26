# Estrategia TP SL Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión directa del asesor experto de MetaTrader 5 "TP SL Trailing". La estrategia no genera entradas por sí misma. En cambio, gestiona posiciones existentes instalando un stop-loss y take-profit de protección y arrastrando el stop una vez que la operación se vuelve rentable. La configuración basada en pips coincide con los parámetros del script original y permite que la lógica se adjunte a cualquier símbolo admitido por StockSharp.

## Lógica de negociación
- Cuando aparece una nueva posición, la estrategia puede opcionalmente establecer un stop-loss y take-profit inicial usando las distancias en pips configuradas. Este comportamiento está controlado por el flag **Only Zero Values**, igual que en el asesor experto original.
- Para posiciones largas, la estrategia mueve el stop-loss hacia arriba una vez que la ganancia no realizada excede la suma del trailing stop y el trailing step. El stop se mueve a `precio actual - trailing stop`, garantizando que se asegure una porción mínima del beneficio.
- Para posiciones cortas, la estrategia refleja la misma idea y mueve el stop hacia abajo una vez que el beneficio excede los umbrales de trailing.
- Si tanto el trailing stop como el trailing step son cero, la estrategia deja el stop-loss sin tocar.
- El nivel de take-profit nunca se arrastra. Solo se establece durante la fase de colocación inicial cuando **Only Zero Values** está habilitado, replicando completamente el comportamiento MQL.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal de las velas usadas para rastrear movimientos de precio. Un marco temporal más rápido mejora la precisión del trailing. |
| `StopLossPips` | Distancia en pips entre el precio de entrada y el stop-loss inicial. Aplicado solo cuando **Only Zero Values** está habilitado. |
| `TakeProfitPips` | Distancia en pips entre el precio de entrada y el take-profit inicial. Aplicado solo cuando **Only Zero Values** está habilitado. |
| `TrailingStopPips` | Distancia de trailing core en pips. Define cuán lejos detrás del precio actual debe permanecer el stop después de la activación. |
| `TrailingStepPips` | Buffer adicional de pips que debe superarse antes de que el stop se mueva de nuevo. Previene actualizaciones de stop demasiado frecuentes. |
| `OnlyZeroValues` | Coincide con el flag EA original. Cuando está habilitado, las órdenes de protección iniciales se crean solo para posiciones que actualmente no tienen stop-loss o take-profit asignados. |

## Notas de conversión
- Las distancias en pips se convierten a unidades de precio usando el `PriceStep` del instrumento. Esto mantiene la lógica agnóstica al instrumento y refleja el ajuste de 3/5 dígitos en la versión MQL.
- Las órdenes de protección se re-registran cada vez que la lógica de trailing mueve el stop-loss. Las órdenes activas de una posición anterior se cancelan automáticamente cuando el tamaño de la posición vuelve a cero.
- Todos los comentarios de código están escritos en inglés, mientras que esta documentación es intencionalmente detallada para ayudar a reproducir cada decisión tomada durante el proceso de porteo.
