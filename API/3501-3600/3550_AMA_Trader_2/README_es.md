# Estrategia AMA Trader 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia AMA Trader 2 replica el flujo de trabajo promedio del experto original MetaTrader de Vladimir Karputov. Combina un filtro de tendencia de media móvil adaptativa (AMA) de Kaufman con un bloque de confirmación de índice de fuerza relativa (RSI). Cuando el precio cierra por encima de la AMA y el RSI cae en territorio de sobreventa, la estrategia agrega exposición larga; la regla simétrica se aplica a operaciones cortas cuando el precio cierra por debajo del AMA mientras RSI imprime una lectura de sobrecompra. Las operaciones promedio se envían en tamaños de lote fijos y pueden restringirse mediante parámetros de riesgo como el recuento máximo de posiciones, el espacio mínimo de entrada y los trailingstops protectores.

## Supuestos del mercado
- **Instrumento**: Diseñado para símbolos FX/CFD negociados con diferenciales ajustados, pero aplicable a cualquier instrumento líquido donde el promedio sea aceptable.
- **Datos**: Opera con velas terminadas basadas en el tiempo. El plazo es configurable a través del parámetro `CandleType` (predeterminado: 1 minuto).
- **Sesiones**: Ventana intradiaria opcional. Las operaciones se pueden limitar a una hora de inicio/finalización en UTC con el indicador `UseTimeWindow`.

## Indicadores
1. **Promedio móvil adaptativo de Kaufman (AMA)**: detecta la tendencia predominante con constantes de suavizado rápido/lento configurables y longitud promedio.
2. **Índice de fuerza relativa (RSI)**: valida los extremos del impulso. El número de lecturas consecutivas de RSI que deben confirmar una señal está controlado por `StepLength` (0 se comporta como 1 y coincide con la versión de MQL).

## Lógica de trading
1. Procese solo velas terminadas y asegúrese de que la estrategia esté en línea y permita operar.
2. Aplique el filtro de tiempo opcional; omitir el procesamiento fuera de la ventana intradiaria cuando esté habilitado.
3. Actualice la cola de valores RSI recientes y calcule los ajustes del trailing-stop para la exposición existente.
4. **Configuración larga**: precio de cierre por encima de AMA y al menos uno de los valores de RSI inspeccionados por debajo de `RsiLevelDown`. Si la posición larga activa está perdiendo dinero, se pone en cola una orden de promedio antes de la entrada estándar, imitando el comportamiento de "recuperación de pérdidas" del asesor experto. Las señales cortas siguen la regla simétrica (`RsiLevelUp`).
5. Las inscripciones honran a `MaxPositions`, `MinStep` y `OnlyOnePosition`. Cuando `CloseOpposite` está habilitado, la estrategia primero compensa al lado contrario y solo considera nuevas entradas después de que se confirma la operación de aplanamiento.
6. Cada nueva posición puede adjuntar distancias fijas de stop-loss/take-profit y, opcionalmente, habilitar un trailing stop basado en ganancias con umbrales de activación, distancia y paso.

## Gestión del riesgo
- **Tamaño de lote fijo**: todas las entradas usan `LotSize`, lo que permite dimensionar la posición a través del parámetro o la cartera de hosting.
- **Profundidad promedio máxima**: `MaxPositions` limita cuántas veces se puede aumentar la exposición por dirección.
- **Control de espaciado**: `MinStep` impone una distancia mínima de precio entre entradas consecutivas, lo que reduce la agrupación en el mismo nivel.
- **Salidas de protección**: la lógica opcional de stop-loss, take-profit y trailing replica el conjunto de herramientas de protección del experto MetaTrader.
- **Exposición opuesta**: `CloseOpposite` obliga a la estrategia a cerrar posiciones cortas antes de abrir posiciones largas (y viceversa). `OnlyOnePosition` garantiza que la estrategia nunca mantenga a ambos lados simultáneamente.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Tipo de datos de vela/período de tiempo utilizado para los cálculos. |
| `LotSize` | Volumen para cada orden de mercado. |
| `RsiLength` | RSI período promedio. |
| `StepLength` | Número de lecturas recientes de RSI inspeccionadas (0 → 1). |
| `RsiLevelUp` | RSI umbral de sobrecompra para entradas cortas. |
| `RsiLevelDown` | RSI umbral de sobreventa para entradas largas. |
| `AmaLength` | Longitud de alisado AMA. |
| `AmaFastPeriod` | Constante de suavizado rápido para AMA. |
| `AmaSlowPeriod` | Constante de suavizado lento para AMA. |
| `StopLoss` | Distancia de parada fija en unidades de precio (0 inhabilitaciones). |
| `TakeProfit` | Distancia objetivo fija en unidades de precio (0 inhabilitaciones). |
| `TrailingActivation` | Beneficio requerido para armar el trailing stop (0 inhabilitaciones). |
| `TrailingDistance` | Distancia mantenida por el trailing stop. |
| `TrailingStep` | Mejora mínima antes de apretar el trailing stop. |
| `MaxPositions` | Entradas promedio máximas por dirección (0 inhabilitaciones). |
| `MinStep` | Distancia mínima entre entradas consecutivas (0 inhabilita). |
| `CloseOpposite` | Cierre la exposición opuesta antes de abrir una operación. |
| `OnlyOnePosition` | Bloquear nuevas entradas siempre que exista alguna posición. |
| `UseTimeWindow` | Habilite el filtrado de hora de inicio/finalización intradía. |
| `StartTime` | Hora de inicio de sesión (UTC) cuando la ventana está habilitada. |
| `EndTime` | Hora de finalización de la sesión (UTC) cuando la ventana está habilitada. |

## Notas de implementación
- Solo API de alto nivel: las velas se suscriben a través de `SubscribeCandles`, AMA y RSI están vinculadas con `.Bind` y todos los cálculos se realizan en la devolución de llamada vinculada sin utilizar captadores de indicadores prohibidos.
- La contabilidad de posiciones refleja al experto MQL: acumuladores separados rastrean volúmenes/precios promedio largos y cortos para evaluar PnL no realizados para tomar decisiones promedio.
- Los trailingstops reconfiguran la distancia de stop-loss a nivel de estrategia en lugar de manipular las colas de órdenes directamente, manteniendo la compatibilidad con el modelo de ejecución StockSharp.
- Las señales están restringidas a una ejecución por barra por lado, reproduciendo la verificación MetaTrader que evita entradas duplicadas en la misma vela.

## Diferencias con el experto MetaTrader
- Se omiten los parámetros específicos de MetaTrader, como números mágicos, desviación, comprobaciones de nivel de congelación y emulación de retiro del probador. El entorno StockSharp gestiona internamente los retrasos en los pedidos y las tarifas.
- Los precios stop/límite se calculan a partir del cierre de la vela en lugar de los ticks de oferta/demanda. Esto coincide con el flujo de trabajo basado en velas de StockSharp.
- El EA original utiliza configuraciones de margen de cuenta para calcular tamaños de lote dinámicos. El puerto mantiene un `LotSize` fijo, dejando el tamaño basado en el riesgo al entorno de alojamiento.
