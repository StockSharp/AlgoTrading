# Estrategia EA Close
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia EA Close** es un port directo de StockSharp del asesor experto MQL5 original "EA Close" creado por Vladimir Karputov. La estrategia combina un Índice de Canal de Materias Primas (CCI), una media móvil ponderada (WMA) y un oscilador estocástico para detectar movimientos de agotamiento al final de los retrocesos. Las órdenes se evalúan solo una vez por vela completada para imitar la lógica de "nueva barra" usada en el EA fuente.

La implementación de StockSharp mantiene el conjunto de parámetros y la estructura de la versión MQL para que las optimizaciones existentes puedan reutilizarse. Las señales se generan desde la vela completada anterior, lo que hace que el comportamiento sea determinista cuando la estrategia se reproduce en datos históricos.

## Indicadores
- **Commodity Channel Index (CCI)** – identifica extremos de sobrecompra y sobreventa relativos al precio promedio durante un período configurable.
- **Weighted Moving Average (WMA)** – actúa como filtro de microtendencia; el EA original usa una LWMA de 1 período del precio ponderado, que en la práctica se comporta como un precio de vela ligeramente suavizado. En este port, la WMA se aplica directamente al flujo de velas.
- **Oscilador Estocástico (línea %K)** – confirma el agotamiento del impulso usando niveles clásicos de sobrecompra y sobreventa.

## Lógica de trading
1. **Configuración larga**
   - El CCI de la vela anterior cae por debajo de `-CciLevel`.
   - El %K estocástico anterior está por debajo de `StochasticLevelDown`.
   - El open de la vela anterior está por encima del valor WMA de esa vela.
   - Si esas condiciones se alinean y la posición neta actual es no positiva, la estrategia compra. La exposición corta existente se compensa antes de abrir la nueva posición larga.
2. **Configuración corta**
   - El CCI de la vela anterior sube por encima de `CciLevel`.
   - El %K estocástico anterior está por encima de `StochasticLevelUp`.
   - El cierre de la vela anterior está por debajo del valor WMA de esa vela.
   - Cuando es verdadero y la posición es no negativa, la estrategia vende. Cualquier posición larga abierta se cierra en la misma orden de mercado.

Solo se usan datos de velas finalizadas. Esto refleja la puerta de nueva barra `OnTick` en el script MQL y previene el repintado intrabarra.

## Gestión de riesgo
`StartProtection` se habilita durante `OnStarted`, reproduciendo las distancias fijas de stop-loss y take-profit del código MQL. Las distancias se configuran en **pips**. El helper convierte pips a unidades de precio multiplicando el paso de precio del instrumento por 10 cuando la precisión del paso tiene tres o cinco lugares decimales (p. ej., 0.001 o 0.00001), coincidiendo con el ajuste de dígitos del EA para cotizaciones de 3/5 dígitos. Establecer una distancia a cero deshabilita ese tramo de protección.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `Volume` | Tamaño de orden usado para entradas de mercado. | 1 |
| `StopLossPips` | Distancia de stop-loss fija medida en pips. | 35 |
| `TakeProfitPips` | Distancia de take-profit fija medida en pips. | 75 |
| `CciPeriod` | Longitud de promediado del indicador CCI. | 14 |
| `CciLevel` | Umbral absoluto que define los extremos del CCI. | 120 |
| `MaPeriod` | Longitud del filtro de media móvil ponderada. | 1 |
| `StochasticLength` | Ventana de look-back para el oscilador estocástico (rango máximo/mínimo). | 5 |
| `StochasticKPeriod` | Factor de suavizado aplicado a la línea %K. | 3 |
| `StochasticDPeriod` | Factor de suavizado aplicado a la línea %D. | 3 |
| `StochasticLevelUp` | Umbral de sobrecompra para la línea %K. | 70 |
| `StochasticLevelDown` | Umbral de sobreventa para la línea %K. | 30 |
| `CandleType` | Serie de velas usada como fuente de datos. | Marco temporal de 1 hora |

## Notas de uso
- La estrategia almacena valores de indicador y precio de la vela finalizada más reciente y evalúa señales en la próxima apertura de barra, replicando la lógica de desplazamiento de array (`CopyBuffer(..., start=1)`) en el EA.
- Las órdenes de mercado tienen un tamaño para aplanar cualquier exposición opuesta y abrir simultáneamente la nueva posición, coincidiendo estrechamente con el helper `ClosePositions` en MQL.
- El `StochasticOscillator` de StockSharp usa `Length` como ventana de look-back, `KPeriod` para el suavizado de %K, y `DPeriod` para el suavizado de %D, equivalente a los parámetros `iStochastic` (K-period, slowing y D-period respectivamente).
- Debido a que StockSharp funciona con velas agregadas en lugar de callbacks de tick, no se requiere lógica adicional de actualización de tasas; la suscripción de datos garantiza que los indicadores reciban velas completas.

## Notas de conversión
- No se proporciona implementación en Python intencionalmente, alineándose con los requisitos de la tarea de conversión.
- La media móvil ponderada opera en la serie de velas; si necesita el precio ponderado exacto de MT5 `(High + Low + 2 * Close) / 4`, procese previamente los valores de vela antes de alimentarlos a la WMA.
- Las órdenes de protección son gestionadas por la plataforma a través de `StartProtection`, por lo que no son necesarias registraciones explícitas de stop/take después de cada operación.
