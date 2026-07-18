# Estrategia de Ruptura Trend Catcher
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Trend Catcher es una conversión del asesor experto de MetaTrader 5 "Trend_Catcher_v2". Combina tres medias móviles exponenciales con el indicador Parabolic SAR para identificar reversiones de tendencia y oportunidades de continuación de tendencia. El sistema opera en un único símbolo y marco temporal y se basa en cálculos al final de la vela, lo que lo hace adecuado para backtesting en StockSharp Designer así como para ejecución en vivo a través de ejecutores basados en la API de StockSharp.

## Indicadores y Filtros
- **Parabolic SAR** — detecta giros alcistas y bajistas que indican posibles reversiones.
- **EMA lenta** — el filtro de tendencia de marco temporal superior que define la dirección dominante.
- **EMA rápida** — reacciona más rápido a los cambios de precio para confirmar la dirección del movimiento actual.
- **EMA de disparo** — mantiene la entrada cerca de la acción del precio y evita operaciones tomadas demasiado lejos de la media.
- **Interruptores de días de trading** — filtros opcionales para deshabilitar el trading en días de la semana seleccionados.

## Lógica de Trading
### Entradas largas
1. El precio de cierre termina por encima del valor actual del Parabolic SAR.
2. La vela anterior cerró por debajo del valor anterior del Parabolic SAR (giro alcista).
3. La EMA rápida está por encima de la EMA lenta, confirmando una tendencia alcista.
4. El precio de cierre está por encima de la EMA de disparo para evitar señales contratendencia.
5. No hay posición abierta y ninguna posición fue cerrada durante la vela actual.

### Entradas cortas
Todas las condiciones anteriores se reflejan:
1. El precio de cierre termina por debajo del valor actual del Parabolic SAR.
2. La vela anterior cerró por encima del valor anterior del Parabolic SAR (giro bajista).
3. La EMA rápida está por debajo de la EMA lenta.
4. El precio de cierre está por debajo de la EMA de disparo.
5. No hay posición abierta y ninguna posición fue cerrada durante la vela actual.

Cuando el interruptor **Reverse Signals** está habilitado, las condiciones largas y cortas se invierten, permitiendo que la estrategia opere rupturas en la dirección opuesta.

## Gestión de Posiciones
- **Stop-loss automático** – cuando está habilitado, el stop se calcula a partir de la distancia entre el precio y el Parabolic SAR multiplicada por el `StopLossCoefficient`. La distancia se limita entre `MinStopLoss` y `MaxStopLoss`.
- **Toma de ganancias automática** – multiplica la distancia del stop por `TakeProfitCoefficient`. Se pueden usar distancias manuales cuando la automatización está deshabilitada.
- **Dimensionamiento de posición basado en riesgo** – el tamaño de la operación se deriva del patrimonio del portafolio y `RiskPercent`. Cuando la operación cerrada más reciente es una pérdida y **Use Martingale** está habilitado, el tamaño calculado se multiplica por `MartingaleMultiplier`.
- **Breakeven y trailing stop** – después de alcanzar el beneficio `BreakevenTrigger`, el stop se mueve al precio de entrada más `BreakevenOffset` (o menos para operaciones cortas). Una vez que la posición gana `TrailingTrigger`, el stop sigue al precio por `TrailingStep`.
- **Cierre en señal opuesta** – cuando está activo, la estrategia sale de una posición existente tan pronto como aparece una configuración opuesta.
- **Una operación por vela** – el algoritmo almacena la marca de tiempo de la última salida y omite entradas hasta que se abre la siguiente vela.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `CandleType` | Marco temporal principal usado para todos los indicadores. | Marco temporal de 15 minutos |
| `CloseOnOppositeSignal` | Salir inmediatamente cuando se detecta la configuración inversa. | `true` |
| `ReverseSignals` | Intercambiar condiciones largas y cortas. | `false` |
| `TradeMonday` … `TradeFriday` | Habilitar o deshabilitar el trading en días de la semana específicos. | `true` |
| `SlowMaPeriod` | Período del filtro de tendencia EMA lenta. | `200` |
| `FastMaPeriod` | Período de la confirmación EMA rápida. | `50` |
| `FastFilterPeriod` | Período de la EMA de disparo. | `25` |
| `SarStep` | Paso de aceleración del Parabolic SAR. | `0.004` |
| `SarMax` | Aceleración máxima del Parabolic SAR. | `0.2` |
| `AutoStopLoss` | Habilitar el cálculo dinámico del stop-loss. | `true` |
| `AutoTakeProfit` | Habilitar el cálculo dinámico de la toma de ganancias. | `true` |
| `MinStopLoss` / `MaxStopLoss` | Límites inferior y superior para la distancia del stop. | `0.001` / `0.2` |
| `StopLossCoefficient` | Multiplicador aplicado a la distancia SAR. | `1` |
| `TakeProfitCoefficient` | Multiplicador usado para la distancia de toma de ganancias. | `1` |
| `ManualStopLoss` | Distancia de stop fija cuando la automatización está deshabilitada. | `0.002` |
| `ManualTakeProfit` | Distancia de objetivo fija cuando la automatización está deshabilitada. | `0.02` |
| `RiskPercent` | Porcentaje del patrimonio del portafolio arriesgado por operación. | `2` |
| `UseMartingale` | Aumentar el tamaño después de una operación perdedora. | `true` |
| `MartingaleMultiplier` | Multiplicador aplicado después de una pérdida. | `2` |
| `BreakevenTrigger` | Beneficio necesario antes de mover el stop al punto de equilibrio. | `0.005` |
| `BreakevenOffset` | Búfer añadido cuando el stop se mueve al punto de equilibrio. | `0.0001` |
| `TrailingTrigger` | Beneficio requerido para comenzar a seguir el stop. | `0.005` |
| `TrailingStep` | Distancia mantenida por el trailing stop. | `0.001` |

## Notas de uso
- La estrategia envía órdenes de mercado tanto para entradas como para salidas; los controles de deslizamiento deben añadirse a nivel del adaptador de corretaje si es necesario.
- Debido a que la lógica usa datos de fin de vela, la precisión de los backtests depende de la granularidad de la serie de velas proporcionada a la estrategia.
- Los parámetros están completamente expuestos a través de objetos `StrategyParam`, lo que los hace disponibles para optimización en StockSharp Designer.
