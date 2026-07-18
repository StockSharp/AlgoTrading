# Estrategia RSI RFTL
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el **RSI RFTL EA** de MetaTrader 5 a la API de alto nivel de StockSharp. Conserva la idea original de operar con líneas de tendencia oscilantes del RSI, mejorada con la Recursive Filter Trend Line (RFTL) como filtro direccional. La implementación reproduce la toma de decisiones barra a barra del experto mientras usa construcciones idiomáticas de StockSharp como `StrategyParam`, enlaces de indicadores y suscripciones de velas.

## Cómo Funciona

1. **Detección de oscilaciones RSI** – se escanean los últimos 500 valores de RSI en busca de máximos y mínimos locales. Los picos deben superar 40 y 60, mientras que los valles deben caer por debajo de 60 y 40, coincidiendo con la lógica de puntos de inflexión de MQL.
2. **Proyección de línea de tendencia** – una vez encontrados dos máximos o mínimos válidos, la estrategia proyecta la correspondiente línea de tendencia del RSI hacia la barra actual y la anterior. Las oscilaciones intermedias que rompen los umbrales 40/60 invalidan la línea, igual que en el experto.
3. **Confirmación RFTL** – el valor anterior de la Recursive Filter Trend Line (calculado con la tabla de coeficientes original) debe estar por encima del cierre anterior para cortos o por debajo para largos. Esto mantiene las entradas alineadas con el filtro RFTL.
4. **Filtrado de entrada** – el RSI también debe mantenerse en el lado apropiado del neutro: los cortos requieren RSI por encima de 47/50, mientras que los largos requieren RSI por debajo de 55/50.
5. **Capa de riesgo** – las distancias de stop de protección, take-profit y trailing stop se expresan en pips y se actualizan en cada vela cerrada, imitando la rutina de modificación de trailing de MQL. Salidas adicionales se activan cuando el RSI supera 70 (cerrar largos) o cae por debajo de 30 (cerrar cortos).

## Lógica de Entrada

- **Configuración corta**
  - Dos mínimos de RSI por debajo de 60/40 definen una línea de tendencia ascendente cuya proyección ahora se rompe a la baja (`RSI[1] < línea`, `RSI[2] > línea(anterior)`).
  - El valor anterior de RFTL está por encima del cierre anterior, confirmando presión descendente.
  - RSI permanece en el lado alcista (`RSI[2] > 50`, `RSI[0] > 47`) y los máximos detectados se encuentran más lejos en la historia que los mínimos (`pos₂ > pos₄`), coincidiendo con la restricción de ordenamiento de MQL.
- **Configuración larga**
  - Dos máximos de RSI por encima de 40/60 definen una línea de tendencia descendente cuya proyección ahora se rompe al alza (`RSI[1] > línea`, `RSI[2] < línea(anterior)`).
  - El valor anterior de RFTL está por debajo del cierre anterior.
  - RSI permanece en el lado bajista (`RSI[2] < 50`, `RSI[0] < 55`) y los mínimos recientes son más recientes que los máximos (`pos₄ > pos₂`).

Las señales se evalúan solo después de que todos los indicadores estén formados y se haya acumulado el historial necesario, evitando operaciones prematuras con datos parciales.

## Gestión de Riesgo

- **Stop Loss / Take Profit** – configurables en pips. Si la vela actual opera más allá del nivel de precio respectivo, la posición se cierra inmediatamente y el estado de trailing se reinicia.
- **Trailing Stop** – opcional. Una vez que el precio se mueve por `TrailingStopPips + TrailingStepPips` a favor de la operación, el stop sigue al cierre mientras se aplica el mismo avance mínimo (`TrailingStepPips`) antes de ajustarse nuevamente.
- **Salida de Emergencia RSI** – los largos se cierran cuando el RSI cruza 70; los cortos se cierran cuando cae por debajo de 30. Esto refleja las salidas hard codificadas en el EA original.

## Parámetros

| Parámetro | Predeterminado | Descripción |
| --- | --- | --- |
| `CandleType` | 1 hora | Marco temporal utilizado para los cálculos de RSI y RFTL. |
| `TradeVolume` | 1 | Volumen de orden enviado en cada entrada. |
| `RsiPeriod` | 30 | Período de lookback del oscilador RSI. |
| `StopLossPips` | 50 | Distancia del stop de protección en pips (0 deshabilita el stop). |
| `TakeProfitPips` | 50 | Distancia del take-profit en pips (0 deshabilita el objetivo). |
| `TrailingStopPips` | 5 | Desplazamiento del trailing stop en pips (0 deshabilita el trailing). |
| `TrailingStepPips` | 5 | Mejora adicional en pips requerida antes de que se actualice el trailing. |

Todas las distancias se multiplican por el `PriceStep` del instrumento, coincidiendo con el manejo de punto/pip de la versión MQL.

## Uso

1. Adjuntar la estrategia a un instrumento y configurar `CandleType` al tamaño de barra usado en sus pruebas de MetaTrader.
2. Ajustar los parámetros de riesgo (stop, take, trailing) a las distancias en pips utilizadas anteriormente. Establecer un parámetro en `0` deshabilita esa protección.
3. Iniciar la estrategia; se suscribirá a las velas especificadas, calculará RSI y RFTL, y comenzará a monitorear señales una vez que se haya recolectado suficiente historial.
4. Monitorear los widgets del gráfico – el área de precio muestra velas y la línea RFTL, mientras que el segundo panel muestra el oscilador RSI.

## Notas y Diferencias

- El indicador RFTL está implementado directamente en C# con la tabla de coeficientes original; no se requieren archivos externos.
- La gestión de operaciones se mantiene en posición única: la estrategia alterna entre largo, corto y plano igual que el EA que solo rastreaba una posición por símbolo/mágico.
- Como los exits de stop y trailing se gestionan dentro de la estrategia (StockSharp no ejecuta automáticamente los stops de MT5), las reentradas se omiten en la barra donde se activa una salida de protección, lo cual es una aproximación conservadora pero segura.
- Los búferes de historial están limitados a 600 registros para reflejar los arreglos de 500 elementos usados en el código fuente y evitar el crecimiento ilimitado de memoria.
- Todos los comentarios inline se reescribieron en inglés y el código sigue las pautas de estilo de la API de alto nivel de StockSharp.
