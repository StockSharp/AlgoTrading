# Estrategia de Cycle Market Order
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Convertida del asesor experto de MetaTrader 4 "CycleMarketOrder_V181". La estrategia organiza un número fijo de slots dentro de una escalera de precios y abre órdenes de mercado cuando el bid/ask en vivo cruza un slot individual. Cada slot lleva su propio volumen, umbral de punto de equilibrio y valor de trailing stop, por lo que la cuadrícula puede escalar gradualmente en una posición mientras protege las ganancias que ya alcanzaron la distancia requerida.

## Lógica de trading

1. El tamaño del pip se deriva del paso de precio del instrumento y la precisión decimal (los símbolos de 5/3 dígitos mapean a 10 puntos por pip). Los parámetros `MaxPrice`, `SpanPips` y `MaxCount` luego se usan para pre-calcular el rango de precios manejado por cada slot.
2. Los datos de mercado de nivel 1 se consumen para imitar el comportamiento basado en ticks del Asesor Experto original. Cada actualización refresca los precios de mejor bid/ask en caché.
3. Si `UseWeekendMode` está habilitado, la estrategia se niega a operar fuera de la ventana de fin de semana configurada (sábado desde `WeekendHour`, todo el domingo y el lunes antes de `WeekstartHour`).
4. Para ciclos largos (`EntryDirection = 1`), el algoritmo escanea los slots de menor a mayor identificador. Siempre que el precio ask actual caiga entre el `startPrice` y `endPrice` del slot, se envía una orden de compra de mercado con volumen `OrderVolume`. Los ciclos cortos (`EntryDirection = -1`) reflejan esta lógica y usan el precio bid.
5. Los estados de los slots rastrean órdenes de entrada/salida pendientes, volumen llenado y el precio de entrada promedio. Los registros usan `MagicNumberBase + index` para coincidir con los identificadores "mágicos" de MT4.
6. La gestión del trailing se ejecuta en cada actualización de nivel 1 antes de evaluar nuevas entradas. Una vez que la ganancia en un slot largo supera `BreakEvenPips + TrailingStopPips`, el stop se empuja a `Bid - TrailingStopPips`. Los slots cortos usan `Ask + TrailingStopPips` y la condición de punto de equilibrio reflejada. Cuando el precio de mercado cruza el stop almacenado, el slot se cierra con una orden de mercado.
7. Dado que solo se usan órdenes de mercado, no hay órdenes pendientes que cancelar. Los llenados parciales ajustan el volumen restante del slot para que la estrategia pueda continuar haciendo trailing o re-armar el slot una vez que quede plano.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `EntryDirection` | Dirección de trading: `1` compra la escalera, `-1` la vende, `0` deshabilita nuevas entradas mientras mantiene el trailing activo. |
| `MaxPrice` | Precio ancla superior usado para calcular los rangos de slots. |
| `MaxCount` | Número total de slots activos dentro de la cuadrícula. |
| `SpanPips` | Distancia en pips entre límites de slots consecutivos. |
| `OrderVolume` | Volumen enviado cuando se activa un slot. |
| `BreakEvenPips` | Distancia de ganancia que debe excederse antes de que se arme el trailing stop. |
| `TrailingStopPips` | Distancia de trailing aplicada una vez que se alcanza el punto de equilibrio. |
| `UseWeekendMode` | Habilita la ventana de bloqueo de trading de fin de semana. |
| `WeekendHour` | Hora del sábado (hora de terminal) cuando se detiene el trading. |
| `WeekstartHour` | Hora del lunes cuando se reanuda el trading. |
| `MagicNumberBase` | Desplazamiento de identificador usado en mensajes de log para coincidir con los números mágicos originales. |

## Notas de implementación

* La gestión de slots rastrea órdenes de entrada y salida pendientes para que los llenados repetidos no registren volumen duplicado.
* La estrategia reinicia su trailing stop cada vez que un nuevo llenado aumenta la exposición del slot, asegurando que el stop refleje el precio de entrada promedio más reciente.
* La protección de fin de semana simplemente omite tanto el trailing como la lógica de entrada; las posiciones existentes permanecen intactas mientras el bloqueo está activo.
* Los datos de nivel 1 son necesarios porque la lógica compara precios brutos de bid/ask en lugar de cierres de velas, replicando de cerca el comportamiento tick a tick de la versión MT4.
