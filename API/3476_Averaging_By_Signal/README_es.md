# Promedio por estrategia de señal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de promedio por señal** transfiere el MetaTrader experto `AveragingBySignal.mq4` al API de alto nivel de StockSharp. El asesor original combinó un filtro de entrada cruzado de promedio móvil con un promedio estilo Martingale, una toma de ganancias de canasta compartida y un trailing stop opcional que solo se activa para el primer pedido. Esta versión de C# recrea los mismos componentes básicos y los adapta al modelo de ejecución de compensación y al marco de indicadores de StockSharp.

## Lógica comercial
1. Suscríbase al período de tiempo configurado (`CandleType`) y proporcione dos promedios móviles creados con los períodos y métodos solicitados (`FastPeriod`/`FastMethod`, `SlowPeriod`/`SlowMethod`).
2. Espere a que las velas estén completamente cerradas. Cuando se completa una barra, compare los valores anteriores y actuales de ambos promedios para detectar un cruce rápido/lento.
3. Generar señales:
   - un cruce alcista (aumento rápido por encima del lento) genera una señal larga;
   - un cruce bajista (caída rápida por debajo de lenta) produce una señal corta;
   - de lo contrario, la estrategia permanece inactiva.
4. Ante una nueva señal larga y mientras no haya ninguna cesta larga activa, envíe una orden de compra de mercado utilizando el volumen base devuelto por el bloque de tamaño de posición.
5. Ante una nueva señal corta y mientras no haya ninguna cesta corta activa, envíe una orden de venta de mercado.
6. Reglas de promedio:
   - la distancia hasta la siguiente capa se controla mediante `LayerDistancePips` convertido a puntos de estilo MetaTrader;
   - las capas largas adicionales requieren una señal alcista (cuando `AveragingBySignal` es verdadera) o solo la condición del precio (cuando es falsa);
   - capas cortas adicionales siguen la lógica simétrica;
   - el tamaño del lote de cada nueva capa se calcula con el modo `LotSizing` y se limita a `MaxLayers` entradas por dirección.
7. Gestión de cesta:
   - cada operación completa se rastrea en orden FIFO para reconstruir el precio de entrada promedio de las cestas largas y cortas;
   - el precio promedio ponderado más/menos `TakeProfitPips` forma la toma de ganancias compartida. Cuando el precio de cierre alcanza ese nivel se cierra toda la cesta;
   - si `EnableTrailing` está habilitado y existe exactamente una orden en una cesta, se activa un trailing stop después de `TrailingStartPips` de ganancia flotante. El stop se adelanta siempre que el precio mejora al menos `TrailingStepPips`.
8. La estrategia funciona en un entorno de compensación: las señales opuestas compensan automáticamente la exposición existente antes de abrir la siguiente canasta.

## Tamaño de posición y cálculo de pips
- `InitialVolume` define el lote base. Cuando `LotSizing` se establece en `Multiplier`, cada capa adicional multiplica el lote base por `Multiplier^layerIndex`, reproduciendo la lógica de MQL `LotType`.
- El asistente ajusta el volumen solicitado a los `VolumeStep`, `MinVolume` y `MaxVolume` del instrumento para que cada pedido cumpla con el intercambio.
- Los valores de pip se derivan de `Security.PriceStep` e imitan el ajuste original de "dos dígitos": los símbolos FX de cinco dígitos usan 0,0001 mientras que los símbolos de cuatro dígitos usan 0,0001 tal como están.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | plazo de 1 hora | Plazo principal para el cálculo de los indicadores. |
| `InitialVolume` | `decimal` | `0.1` | Tamaño de lote base para el primer pedido en una cesta. |
| `LotSizing` | `LotSizingMode` | `Multiplier` | Elija entre lotes fijos o escalamiento geométrico. |
| `Multiplier` | `decimal` | `2` | Multiplicador de lote aplicado a cada capa adicional cuando `LotSizing` = `Multiplier`. |
| `FastPeriod` | `int` | `28` | Vista retrospectiva de la media móvil rápida. |
| `FastMethod` | `MovingAverageMethod` | `LinearWeighted` | Método de media móvil para la línea rápida. |
| `SlowPeriod` | `int` | `50` | Vista retrospectiva de la media móvil lenta. |
| `SlowMethod` | `MovingAverageMethod` | `Smoothed` | Método de media móvil para la línea lenta. |
| `TakeProfitPips` | `int` | `15` | Distancia de toma de ganancias compartida para toda la cesta (0 inhabilitaciones). |
| `AveragingBySignal` | `bool` | `true` | Requiere una nueva señal antes de agregar nuevas capas. |
| `LayerDistancePips` | `decimal` | `10` | Movimiento adverso mínimo (en pips) antes del promedio. |
| `MaxLayers` | `int` | `10` | Órdenes máximas simultáneas por sentido, incluida la inicial. |
| `EnableTrailing` | `bool` | `false` | Habilite el trailing stop para cestas de un solo pedido. |
| `TrailingStartPips` | `decimal` | `10` | Se requiere ganancia flotante antes de que comience el seguimiento. |
| `TrailingStepPips` | `decimal` | `1` | Se necesita progreso adicional para mover el trailing stop. |

## Diferencias con el asesor experto original
- StockSharp opera en modo de compensación, mientras que MetaTrader 4 permite posiciones de cobertura independientes. Cuando una señal cambia de dirección, el nuevo orden de mercado compensa la exposición existente antes de crear una nueva cesta.
- La toma de ganancias compartida se implementa como un comando de salida explícito en lugar de modificar cada ticket con `OrderModify`.
- El trailing stop se modela con salidas del mercado provocadas por los precios de cierre de las velas. El experto original se basó en actualizaciones de parada a nivel de tick; por lo tanto, la versión C# puede tardar un poco más, pero sigue los mismos umbrales.
- Las comprobaciones de riesgos como `AccountFreeMarginCheck` y el manejo de deslizamiento se omiten porque los corredores StockSharp aplican reglas de margen/precio directamente.

## Consejos de uso
- Proporcione metadatos precisos del instrumento (`PriceStep`, `VolumeStep`, volumen mínimo y máximo) para conversiones correctas de pips y volumen.
- Mantenga `FastPeriod` estrictamente por debajo de `SlowPeriod`; la estrategia se detiene automáticamente si la configuración impide cruces válidos.
- Desactive `AveragingBySignal` cuando desee una red pura que reaccione únicamente a los niveles de precios, independientemente del último cruce.
- Debido a que la lógica de salida opera con velas cerradas, los marcos de tiempo más bajos producen reacciones más rápidas pero también pueden aumentar el ruido y el número de capas promedio.
