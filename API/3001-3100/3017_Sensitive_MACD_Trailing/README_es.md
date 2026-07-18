# Estrategia Sensitive MACD con Trailing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
Esta estrategia es una conversión directa a StockSharp del asesor experto MACD "Sensitive" para MetaTrader 5. Combina cruces del MACD con herramientas de gestión de riesgo configurables (stop loss fijo, take profit y trailing stops basados en pips). El algoritmo funciona exclusivamente en velas completadas y usa la API de alto nivel para suscribirse al marco temporal deseado.

## Indicadores y Datos
- **MACD (Moving Average Convergence Divergence)** – configurado con longitudes de EMA rápida, lenta y de señal independientes.
- **Velas** – marco temporal seleccionable por el usuario proporcionado a través del parámetro `CandleType`.

## Condiciones de Entrada
1. Debe cerrarse una nueva vela para evitar ruido intrabarra.
2. Los valores del MACD se procesan desde la vinculación del indicador:
   - `macd` representa la línea principal del MACD.
   - `signal` es la línea de señal (EMA de la diferencia MACD).
3. Requisitos de **entrada larga**:
   - El MACD cruza por encima de la línea de señal (`macd > signal` mientras los valores anteriores satisfacían `macd < signal`).
   - El MACD permanece en territorio negativo (`macd < 0`).
   - La magnitud absoluta del MACD es mayor que `MacdOpenLevel * Point`, asegurando un desplazamiento significativo.
   - No hay posición larga activa (la posición neta es menor o igual a cero). Los cortos existentes se revierten enviando la cantidad necesaria.
4. Los requisitos de **entrada corta** son un espejo de la configuración larga:
   - El MACD cruza por debajo de la línea de señal permaneciendo positivo.
   - La magnitud absoluta del MACD supera el umbral configurado.
   - No existe posición corta abierta (la posición neta es mayor o igual a cero). Los largos existentes se aplanan antes de abrir el corto.

## Gestión de Salida
- **Take Profit**: Una vez abierta la operación, la estrategia almacena un nivel objetivo definido por `TakeProfitPips`. Si el máximo de una vela larga o el mínimo de una vela corta alcanza este nivel, la posición se cierra a mercado.
- **Stop Loss**: Se calcula un stop de protección a partir de `StopLossPips`. Para largos, una caída del precio al nivel de stop activa una salida a mercado. Los cortos reaccionan a subidas del precio que alcanzan el stop.
- **Trailing Stop**: Cuando `TrailingStopPips` es distinto de cero, el algoritmo activa una lógica de trailing después de que el precio avanza al menos `TrailingStopPips + TrailingStepPips` pips desde la entrada. Los movimientos posteriores ajustan el nivel de stop manteniendo siempre la distancia de trailing especificada desde el último cierre. El paso de trailing debe ser positivo siempre que el trailing stop esté habilitado; de lo contrario, la estrategia se detiene con un mensaje de error.
- Cuando no hay posición activa, las variables de seguimiento internas se restablecen para prepararse para la próxima operación.

## Dimensionamiento de Posición
Las cantidades de las órdenes se controlan mediante el parámetro de estrategia integrado `Volume` (predeterminado: 0.1). Las reversiones añaden automáticamente el valor absoluto de la posición actual al volumen deseado para cambiar de dirección en una sola orden de mercado.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `FastLength` | Período de EMA rápida utilizado por la línea principal del MACD. | 12 |
| `SlowLength` | Período de EMA lenta utilizado por la línea principal del MACD. | 26 |
| `SignalLength` | Período de EMA de señal para el MACD. | 9 |
| `MacdOpenLevel` | Magnitud mínima del MACD (en puntos de precio) requerida para activar operaciones. | 3 |
| `StopLossPips` | Distancia del stop de protección en pips. | 35 |
| `TakeProfitPips` | Distancia de take-profit en pips. | 75 |
| `TrailingStopPips` | Distancia de trailing stop en pips (0 deshabilita el trailing). | 5 |
| `TrailingStepPips` | Distancia adicional que debe moverse el precio antes de que se actualice el trailing stop. | 5 |
| `CandleType` | Tipo de vela fuente (marco temporal). | Velas de 1 minuto |
| `Volume` | Volumen de la orden, expresado en lotes/contratos según el instrumento. | 0.1 |

## Notas Adicionales
- Los valores de pips y puntos del MACD se derivan del paso de precio del instrumento y su precisión decimal. El código ajusta los símbolos forex de 3 y 5 dígitos escalando el tamaño del pip en consecuencia.
- Todos los comentarios dentro del código fuente están escritos en inglés, y la implementación usa solo las APIs de alto nivel de StockSharp de acuerdo con las directrices del proyecto.
- La estrategia evita intencionalmente la gestión de llenados parciales y asume que las órdenes de mercado se ejecutan inmediatamente al ejecutarse en el simulador o en trading real. Se pueden agregar salvaguardas adicionales si es necesario.
