# Estrategia Alexav D1 Profit GBPUSD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de ruptura diario para GBPUSD que combina una EMA calculada en máximos, filtros RSI, confirmación de momentum MACD y gestión de riesgo basada en ATR. El script reproduce el comportamiento de cuatro tomas de ganancias y punto de equilibrio de la versión MetaTrader original.

## Datos Clave

- **Mercado**: GBP/USD spot o CFD
- **Período**: Velas diarias (configurable)
- **Dirección**: Largo y corto
- **Estilo de posición**: Escalado multi-objetivo con stop-loss compartido
- **Instrumentos Usados**: EMA (High), RSI, línea principal MACD, ATR

## Configuración de Indicadores

1. **EMA en precios Máximos** – longitud predeterminada 6, aproxima el nivel dinámico de ruptura.
2. **RSI** – longitud predeterminada 10, define los corredores de sobrecompra/sobreventa usados como filtros de momentum.
3. **Línea principal MACD** – rápido 5, lento 21, señal 14. Solo se usa la línea principal para medir la pendiente del momentum.
4. **ATR** – longitud 28, proporciona stops y objetivos dependientes de la volatilidad.

## Lógica de Entrada

### Entradas Largas

1. La barra diaria anterior abre por debajo de la EMA (High) y cierra por encima de ella (confirmación de cruce ascendente).
2. El RSI se mantiene entre **60** y **80** – previene trades durante momentum débil y evita rallys sobreextendidos.
3. La línea principal del MACD satisface una de dos verificaciones de momentum:
   - El valor hace dos barras es negativo (indicando que la tendencia recientemente se volvió positiva), **o**
   - La reducción relativa en MACD absoluto entre las últimas dos barras supera el umbral configurable **MacdDiffBuy** (predeterminado 0.5).

Si todas las condiciones se cumplen, se colocan cuatro órdenes iguales de compra de mercado (0.1 lotes cada una por defecto). Cualquier exposición corta existente se aplana antes de enviar el nuevo lote.

### Entradas Cortas

1. La barra abre por encima de la EMA (High) y cierra por debajo de ella.
2. El RSI está entre **25** y **39** – refleja los umbrales del lado largo.
3. El MACD hace dos barras es positivo **o** el cambio relativo en el MACD absoluto entre las últimas dos barras está por encima de **MacdDiffSell** (predeterminado 0.15).

En confirmación, la estrategia aplana los largos existentes, luego envía cuatro ventas iguales de mercado.

## Gestión de Trades

- **Stop Inicial**: Stop ATR compartido calculado desde el cierre de entrada. Los largos usan `entry - ATR * StopLossMultiplier` (predeterminado 1.6). Los cortos usan `entry + ATR * StopLossMultiplier`.
- **Objetivos de Ganancia**: Cuatro niveles incrementales basados en ATR por dirección: múltiplos `1.0`, `1.5`, `2.0` y `2.5` escalados por el parámetro `TakeProfitMultiplier` (predeterminado 1). Cada nivel cierra un cuarto de la posición original a través de una orden de mercado cuando el precio opera a través del nivel.
- **Comportamiento de Punto de Equilibrio**: Después de cada salida parcial el stop protector para la posición restante se mueve al precio objetivo más reciente. Esto imita el EA original que modifica los stop-loss al precio de take-profit ejecutado cada vez que ocurre un trade TP.
- **Manejo de Stop**: Si el precio toca el nivel protector intrabarra (usando máximo/mínimo de vela), la posición restante se cierra inmediatamente a mercado.

## Notas de Control de Riesgo

- La estrategia no piramida más allá del lote de cuatro entradas. Una nueva señal se ignora mientras la exposición permanece en la misma dirección.
- El ATR debe ser positivo; las señales se omiten si el indicador de volatilidad aún no se ha formado.
- Los cambios de parámetros en tiempo de ejecución afectan solo órdenes futuras; el volumen por orden se captura en la entrada para el escalado correcto en las salidas.

## Parámetros

| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `OrderVolume` | Volumen por orden individual de mercado en el lote | `0.1` |
| `EmaPeriod` | Longitud de EMA aplicada a máximos de vela | `6` |
| `RsiPeriod` | Período de promedio RSI | `10` |
| `AtrPeriod` | Período de promedio ATR | `28` |
| `StopLossMultiplier` | Múltiplo ATR para el stop protector | `1.6` |
| `TakeProfitMultiplier` | Múltiplo ATR base para objetivos de ganancia | `1.0` |
| `MacdFastPeriod` | Longitud de EMA rápida del MACD | `5` |
| `MacdSlowPeriod` | Longitud de EMA lenta del MACD | `21` |
| `MacdSignalPeriod` | Longitud de EMA de señal del MACD | `14` |
| `MacdDiffBuyThreshold` | Mejora mínima de pendiente MACD para trades largos | `0.5` |
| `MacdDiffSellThreshold` | Mejora mínima de pendiente MACD para trades cortos | `0.15` |
| `RsiUpperLimit` | RSI máximo permitido antes de una entrada larga | `80` |
| `RsiUpperLevel` | RSI mínimo requerido para una entrada larga | `60` |
| `RsiLowerLevel` | RSI máximo permitido para una entrada corta | `39` |
| `RsiLowerLimit` | RSI mínimo requerido antes de cortos | `25` |
| `CandleType` | Marco temporal usado para la suscripción de velas | `1 Day` |

## Consejos de Despliegue

- Optimizar los umbrales de RSI y MACD juntos; aflojar los corredores RSI sin ajustar los filtros de aceleración MACD puede crear falsas señales.
- Dado que las salidas parciales dependen de los extremos de la vela, los datos precisos de valores máximos/mínimos son importantes para backtests realistas.
- Siempre operar con capital suficiente para manejar cuatro órdenes simultáneas por señal.
