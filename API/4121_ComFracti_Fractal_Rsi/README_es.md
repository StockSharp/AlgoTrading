# Estrategia ComFracti Fractal RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia ComFracti Fractal RSI es una versión StockSharp del experto MetaTrader *ComFracti*. El algoritmo busca sesgo direccional utilizando fractales de Bill Williams en dos períodos de tiempo y filtra las señales con un rápido RSI calculado en velas diarias. Una vez que aparece una configuración válida, la estrategia abre una única posición, la protege con distancias configurables de stop-loss y take-profit y, opcionalmente, puede salir cuando la señal se invierte o cuando se alcanza un límite de tiempo de tenencia.

La configuración predeterminada replica el período de negociación de 15 minutos con un período de confirmación de 1 hora y una duración diaria RSI de tres períodos utilizando el precio de apertura de la vela, al igual que el experto original.

## Lógica de trading
1. **Detección de sesgo fractal**
   - Las velas terminadas del período de negociación y del período superior se procesan a través de una ventana fractal de cinco barras.
   - Los parámetros `Primary*Shift` y `Higher*Shift` definen cuántas barras retroceden la estrategia para detectar el último fractal confirmado. Los valores predeterminados coinciden con el valor original de `3`, lo que significa que el código evalúa el fractal que se confirmó hace tres velas.
   - Un fractal bajista (baja) sin un fractal alcista que lo acompañe se trata como alcista (+1). Un fractal alcista sin un fractal bajista se trata como bajista (-1).
2. **Filtro diario RSI**
   - Un `RelativeStrengthIndex` con el `RsiPeriod` configurable (predeterminado `3`) se ejecuta en el período de tiempo diario y utiliza el precio de apertura de la vela, coincidiendo con la implementación de MetaTrader.
   - Las configuraciones largas requieren que RSI esté por debajo de `50 - RsiBuyOffset`; las configuraciones cortas requieren que RSI esté por encima de `50 + RsiSellOffset`.
3. **Condiciones de entrada**
   - **Comprar**: Ambos rastreadores de fractales informan +1 y el filtro RSI es alcista. La estrategia abre una posición larga si es plana o corta, enviando suficiente volumen para girar hacia el lado largo.
   - **Vender**: Ambos rastreadores de fractales informan -1 y el filtro RSI es bajista. La estrategia abre una posición corta si es plana o larga, enviando suficiente volumen para volcarse hacia el lado corto.
4. **Gestión de posiciones**
   - Los niveles protectores de stop-loss y take-profit se calculan inmediatamente después de que cambia la posición en función de `StopLossPips` y `TakeProfitPips` multiplicados por el tamaño del pip del instrumento.
   - La posición se puede cerrar cuando el precio alcanza el stop o el objetivo, cuando transcurre `ExpiryMinutes` o cuando `CloseOnOppositeSignal` esté habilitado y la señal se invierta.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ---- | ----------- | ------- |
| `Volume` | Volumen de pedidos utilizado para cada entrada. | `0.1` |
| `TakeProfitPips` | Distancia objetivo de ganancias en pips. Establezca en `0` para desactivar. | `700` |
| `StopLossPips` | Distancia de stop-loss en pips. Establezca en `0` para desactivar. | `2500` |
| `ExpiryMinutes` | Tiempo máximo de espera en minutos antes de forzar una salida. `0` desactiva el temporizador. | `5555` |
| `CloseOnOppositeSignal` | Cierre la posición activa cuando la señal cambie en la dirección opuesta. | `false` |
| `PrimaryBuyShift` | Las barras retroceden para inspeccionar el fractal alcista en el período de negociación. | `3` |
| `HigherBuyShift` | Las barras retroceden para inspeccionar el fractal alcista en el marco temporal superior. | `3` |
| `PrimarySellShift` | Las barras retroceden para inspeccionar el fractal bajista en el período de negociación. | `3` |
| `HigherSellShift` | Las barras retroceden para inspeccionar el fractal bajista en el marco temporal superior. | `3` |
| `RsiBuyOffset` | Se requiere una compensación inferior a 50 para configuraciones largas. | `3` |
| `RsiSellOffset` | Se requiere una compensación superior a 50 para configuraciones cortas. | `3` |
| `RsiPeriod` | RSI duración en el período de tiempo diario. | `3` |
| `CandleType` | Tipo de vela de marco temporal de negociación. | velas de 15 minutos |
| `HigherTimeFrame` | Tipo de vela del plazo de confirmación. | velas de 1 hora |
| `DailyTimeFrame` | Tipo de vela utilizada para el RSI diario. | velas de 1 dia |

## Notas de implementación
- La estrategia utiliza la suscripción de vela de alto nivel API (`SubscribeCandles().Bind(...)`) y gestiona los indicadores internamente sin exponerlos a través de `Strategy.Indicators`, como lo exigen las pautas.
- Fractals se calculan a través de un asistente interno que almacena una ventana móvil de cinco velas y actualiza la señal solo después de que se confirma un fractal.
- Los valores RSI se recuperan a través de `RelativeStrengthIndex.Process(...)` con el precio de apertura de la vela, coincidiendo con el modo MetaTrader `PRICE_OPEN`.
- Sólo se mantiene una posición a la vez. Las órdenes de mercado invierten la posición cuando es necesario añadiendo el volumen necesario para cubrir una exposición existente.
- El tamaño del pip se estima a partir de `Security.PriceStep` y `Security.Decimals`, utilizando un multiplicador de 10x para activos cotizados con tres o más decimales, reproduciendo la conversión de MetaTrader `Point` a pip.

## Consejos de uso
- Los cambios fractales deben ser lo suficientemente grandes como para garantizar que exista el índice de vela solicitado. Con un cambio de tres, el rastreador requiere al menos cinco velas terminadas por período de tiempo antes de generar señales.
- Cuando opere con instrumentos con diferentes tamaños de tick (por ejemplo, índices o acciones), ajuste `TakeProfitPips` y `StopLossPips` para que coincidan con la definición de pip del instrumento.
- Al deshabilitar `CloseOnOppositeSignal` se replica el comportamiento original del asesor experto (estaba deshabilitado de manera predeterminada) y se basa únicamente en paradas, objetivos o el temporizador de vencimiento para las salidas.
- La estrategia no implementa martingala ni dimensionamiento basado en riesgos; el cálculo del lote MetaTrader se basó en funciones de margen de cuenta que no están disponibles en StockSharp. Utilice el parámetro `Volume` o ajuste la estrategia en un administrador de cartera si se requiere un tamaño de posición dinámico.
