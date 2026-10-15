# Estrategia de TenPips Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **estrategia TenPips** es un port de StockSharp del asesor experto de MetaTrader "10PIPS". Combina medias móviles ponderadas linealmente rápidas/lentas calculadas en el marco temporal de trading con una confirmación de Momentum multi-marco temporal y un filtro MACD macro (mensual). La conversión refleja el módulo de gestión de capital original, incluyendo protección de punto de equilibrio, trailing en pips y objetivos de ganancia en capital/absolutos.

## Lógica de señales

1. **Marco temporal primario** (parámetro `CandleType`, por defecto 15 minutos) suministra el flujo de precios utilizado para las LWMAs rápidas y lentas calculadas en el precio típico `(H + L + C) / 3`.
2. **Confirmación de Momentum en marco temporal superior** (`MomentumCandleType`, por defecto 1 hora) convierte la diferencia de Momentum de StockSharp en la proporción de MetaTrader. La distancia absoluta desde `100` en las últimas tres barras completadas debe superar `MomentumThreshold` para que una operación se arme.
3. **Filtro MACD macro** (`MacdCandleType`, por defecto velas de 30 días aproximando el período mensual de MetaTrader) requiere que la línea principal del MACD esté por encima de la línea de señal para compras y por debajo para ventas.

Se abre una posición larga cuando la vela anterior:
- cerró por encima de la LWMA rápida después de caer por debajo de ella,
- la LWMA rápida está por encima de la LWMA lenta,
- cualquiera de las últimas tres lecturas de Momentum cumple `MomentumThreshold`,
- el MACD macro es alcista.

Una posición corta utiliza las condiciones simétricas (cierre anterior por debajo de la LWMA rápida, rápida por debajo de lenta, Momentum por encima del umbral, MACD bajista).

Dado que StockSharp opera con un modelo de posición neta, el port abre como máximo una posición agregada por lado. Enviar una compra mientras se está corto cierra automáticamente la porción corta y deja el volumen largo solicitado.

## Gestión de riesgo y capital

- **Distancias de protección** – `StopLossPips` y `TakeProfitPips` traducen los pips de MetaTrader en offsets de precio usando el `PriceStep` del instrumento. Cuando cualquier límite es alcanzado, la estrategia cierra toda la posición con una orden de mercado.
- **Trailing stop** – `TrailingStopPips` sigue el precio más alto (largo) o más bajo (corto) desde la entrada.
- **Punto de equilibrio** – cuando está habilitado, `BreakEvenTriggerPips` arma el stop y lo desplaza a la entrada más el opcional `BreakEvenOffsetPips`.
- **Objetivos monetarios** – el trío `UseMoneyTakeProfit`, `UsePercentTakeProfit` y `EnableMoneyTrailing` replica el `TP_In_Money`, `TP_In_Percent` del EA y el bloqueo de trailing basado en balance. Las PnL no realizadas se miden al cierre de cada vela.
- **Stop de capital** – `UseEquityStop` con `EquityRiskPercent` implementa la protección original `UseEquityStop` / `TotalEquityRisk` cerrando posiciones cuando el drawdown desde el pico de capital supera el umbral.
- **Flag de salida MACD** – `UseMacdExit` refleja el interruptor `Exit` del EA, cerrando posiciones anticipadamente cuando el MACD macro gira contra la operación.

## Parámetros

| Parámetro | Por defecto | Descripción |
|-----------|---------|-------------|
| `TradeVolume` | `0.01` | Volumen de posición neta usado para órdenes de mercado (equivalente al tamaño de lote de MetaTrader). |
| `CandleType` | Marco temporal `15m` | Marco temporal primario para las LWMAs rápidas/lentas y la ejecución de operaciones. |
| `MomentumCandleType` | Marco temporal `1h` | Velas de marco temporal superior alimentando la confirmación de Momentum. |
| `MacdCandleType` | Marco temporal `30d` | Marco temporal macro (aproximación mensual) para la confirmación MACD. |
| `FastMaPeriod` | `8` | Período de la media móvil ponderada linealmente rápida. |
| `SlowMaPeriod` | `50` | Período de la media móvil ponderada linealmente lenta. |
| `MomentumPeriod` | `14` | Lookback para la proporción de Momentum. |
| `MomentumThreshold` | `0.3` | Distancia absoluta mínima desde `100` (Momentum de MetaTrader) requerida en las últimas tres barras de marco temporal superior. |
| `StopLossPips` | `20` | Stop-loss de protección en pips de MetaTrader. Establecer en cero para deshabilitar. |
| `TakeProfitPips` | `50` | Take-profit de protección en pips de MetaTrader. Establecer en cero para deshabilitar. |
| `TrailingStopPips` | `40` | Distancia del trailing stop en pips (cero deshabilita el trailing). |
| `UseBreakEven` | `true` | Habilita el comportamiento de mover al punto de equilibrio. |
| `BreakEvenTriggerPips` | `30` | Ganancia (pips) requerida antes de que se active el punto de equilibrio. |
| `BreakEvenOffsetPips` | `30` | Pips adicionales añadidos al stop de punto de equilibrio una vez activado. |
| `UseMoneyTakeProfit` | `false` | Cierra posiciones al alcanzar el objetivo de ganancia absoluta `MoneyTakeProfit`. |
| `MoneyTakeProfit` | `10` | Objetivo de ganancia expresado en moneda de cuenta. |
| `UsePercentTakeProfit` | `false` | Cierra posiciones al ganar el porcentaje `PercentTakeProfit` del capital inicial. |
| `PercentTakeProfit` | `10` | Objetivo porcentual basado en el capital inicial. |
| `EnableMoneyTrailing` | `true` | Habilitar trailing stop basado en balance usando `MoneyTrailTarget` / `MoneyTrailStop`. |
| `MoneyTrailTarget` | `40` | Ganancia (moneda) requerida antes de que se arme el money trail. |
| `MoneyTrailStop` | `10` | Retroceso permitido después de armar el money trail. |
| `UseEquityStop` | `true` | Habilitar protección de drawdown de capital. |
| `EquityRiskPercent` | `1` | Drawdown máximo desde el pico de capital antes de forzar posición plana. |
| `UseMacdExit` | `false` | Cerrar posiciones en una señal MACD opuesta del marco temporal macro. |

## Notas de implementación

- La conversión de pips sigue la lógica del EA: si el tick size del bróker es `0.00001` o `0.001`, un pip equivale a diez ticks; de lo contrario se usa el `PriceStep` bruto.
- El indicador de Momentum de StockSharp produce una diferencia de precio. La estrategia lo convierte a la proporción de MetaTrader `(Close / Close(period) * 100)` antes de aplicar `MomentumThreshold`.
- El port opera en un entorno de netting y por lo tanto no replica el martingale multi-ticket del EA (`IncreaseFactor`, `LotExponent`, `Max_Trades`). En cambio, ajusta el volumen de la orden automáticamente al cambiar entre posiciones largas y cortas.
- Las salidas protectoras y la gestión de ganancia envían órdenes de mercado, coincidiendo con el comportamiento del advisor original al modificar tickets abiertos.
- Los gráficos muestran los indicadores procesados (LWMA rápida, LWMA lenta, Momentum, MACD) cuando la visualización está disponible.

## Uso

1. Configura los marcos temporales de velas para que coincidan con el gráfico de MetaTrader y el marco temporal superior utilizado por el EA.
2. Ajusta los parámetros de riesgo basados en pips al tamaño de punto del instrumento. Cero deshabilita el componente correspondiente.
3. Habilita o deshabilita los objetivos monetarios/porcentuales, el stop de capital y la salida MACD según tus preferencias de riesgo.
4. Inicia la estrategia; se suscribirá a los tres marcos temporales requeridos, gestionará posiciones de acuerdo con las reglas originales y registrará las salidas protectoras activadas por las protecciones basadas en balance o capital.
