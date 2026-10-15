# Estrategia Autotrade con Stops Pendientes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión en C# del asesor experto de MetaTrader *Autotrade (edición de barabashkakvn)*. Mantiene continuamente dos órdenes de entrada por stop simétricas alrededor del precio de mercado actual. Siempre que el mercado permanezca flat y no haya posición abierta, la estrategia actualiza ambas órdenes pendientes. Cuando se ejecuta una orden stop, la posición se monitorea activamente: las salidas se activan cuando la acción del precio se estabiliza o cuando se alcanza un umbral absoluto de ganancia/pérdida. La implementación usa la API de alto nivel de StockSharp según lo requerido por las directrices del proyecto.

## Correspondencia con los parámetros originales
| Parámetro StockSharp | Parámetro MQL5 | Descripción |
| --- | --- | --- |
| `IndentTicks` | `InpIndent` | Distancia (en pasos de precio) entre el precio actual y las órdenes de entrada stop. |
| `MinProfit` | `MinProfit` | Ganancia flotante mínima (moneda de la cuenta) necesaria para salir durante una fase tranquila del mercado. |
| `ExpirationMinutes` | `ExpirationMinutes` | Tiempo de vida de las órdenes stop pendientes antes de ser canceladas y recreadas. |
| `AbsoluteFixation` | `AbsoluteFixation` | Nivel absoluto de ganancia o pérdida (moneda) que fuerza el cierre de la posición. |
| `StabilizationTicks` | `InpStabilization` | Tamaño máximo del cuerpo de la vela anterior que se trata como zona de consolidación. |
| `OrderVolume` | `Lots` | Volumen utilizado tanto para el buy stop como para el sell stop. |
| `CandleType` | `Period()` | Serie de velas que impulsa la lógica (marco temporal de 1 minuto por defecto). |

Todos los parámetros numéricos que representan distancias de precio se convierten de "puntos" a pasos de precio reales mediante el valor `Security.PriceStep`. Los umbrales basados en ganancias se calculan usando `Security.StepPrice`, lo que refleja los cálculos de ganancia de MQL que operan en la moneda del depósito.

## Lógica de trading
### Despliegue de órdenes pendientes
1. La estrategia reacciona solo a velas terminadas (`CandleStates.Finished`).
2. La primera vela se usa para sembrar datos históricos (open/close anterior) e inmediatamente programar órdenes pendientes.
3. Cuando no hay posición abierta, se limpian las referencias inactivas y:
   - Se coloca un buy stop en `Close + IndentTicks * PriceStep`.
   - Se coloca un sell stop en `Close - IndentTicks * PriceStep`.
4. Cada orden pendiente recibe una marca de tiempo de expiración igual a `CloseTime + ExpirationMinutes` minutos. Cuando se alcanza ese tiempo, la orden se cancela y se recrea en la siguiente vela.

### Gestión de posición
1. Una vez ejecutada cualquiera de las órdenes stop, la orden pendiente contraria se cancela para evitar coberturas no deseadas en el modelo de cuenta basado en netting de StockSharp.
2. La estrategia guarda el cuerpo de la vela anterior (`|Open - Close|`) para detectar condiciones tranquilas del mercado.
3. Para cada vela con una posición abierta:
   - La ganancia no realizada se estima en moneda usando la diferencia de precio respecto a `PositionAvgPrice`, escalada por `Security.PriceStep` y `Security.StepPrice`.
   - Si la ganancia supera `MinProfit` **y** el cuerpo de la vela anterior está por debajo de `StabilizationTicks * PriceStep`, la posición se cierra a mercado.
   - Independientemente de la estabilización, si la ganancia o pérdida absoluta supera `AbsoluteFixation`, la posición también se cierra a mercado.
4. Cuando la posición vuelve a flat, todas las órdenes pendientes restantes se eliminan.

### Comportamientos adicionales
- Solo se permite una posición a la vez; los volúmenes de órdenes se netean usando `OrderVolume`.
- Como StockSharp no expone bid/ask durante los backtests de la misma forma que MetaTrader, el precio de cierre de la vela completada se usa como nivel de referencia para nuevas órdenes stop.
- La estrategia actualiza automáticamente el valor `Volume` en caché cuando `OrderVolume` se ajusta mediante parámetros u optimización.

## Notas de implementación y diferencias
- Los cálculos de ganancia dependen de `Security.PriceStep` y `Security.StepPrice`. Asegúrate de que estos campos estén completos en los metadatos del instrumento; de lo contrario, el valor `1` se usa como fallback.
- La versión MQL original permitía cobertura temporal (múltiples órdenes en direcciones opuestas). El port de StockSharp cancela el stop no utilizado inmediatamente después de una ejecución para cumplir con el modelo de netting de la plataforma.
- La expiración de órdenes pendientes usa el `CloseTime` de la vela. Si los datos históricos carecen de marcas de tiempo de cierre, ajusta el feed para proporcionarlas o extiende el código en consecuencia.
- La estrategia funciona con cualquier tipo de dato de velas ajustando `CandleType`. Las velas predeterminadas están basadas en marco temporal (`TimeSpan.FromMinutes(1).TimeFrame()`).

## Recomendaciones de uso
1. Configura la serie de velas que coincida con el período del gráfico utilizado en MetaTrader.
2. Establece `IndentTicks`, `StabilizationTicks` y los umbrales de ganancia en relación con el tamaño del tick y el valor del tick del instrumento.
3. Verifica que el portafolio use cobertura o netting según se desee. La estrategia asume netting y cerrará el libro antes de volver a armar las órdenes stop.
4. Usa los parámetros proporcionados para optimización en StockSharp Designer o Backtester para adaptar el comportamiento a diferentes mercados.
5. Monitorea la salida del log: el código depende de velas terminadas y disponibilidad del mercado (`IsFormedAndOnlineAndAllowTrading()`) antes de enviar nuevas órdenes.

## Aviso de riesgo
El trading automatizado implica un riesgo sustancial. Realiza backtests exhaustivos, valida los parámetros en datos históricos y confirma los requisitos específicos del broker (como distancias mínimas para órdenes stop) antes de implementar la estrategia en una cuenta real.
