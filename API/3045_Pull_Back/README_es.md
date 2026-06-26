# Estrategia de Retroceso (Pull Back)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general

La estrategia Pull Back reproduce la lógica del asesor experto original de MetaTrader "PULL BACK" usando las APIs de alto nivel de StockSharp. El enfoque busca retrocesos hacia una media móvil ponderada rápida en un marco temporal superior, confirma la fuerza del momentum a través de varias barras y opera en la dirección de la tendencia mensual del MACD. Una vez que una posición está abierta, el algoritmo aplica reglas de gestión de dinero que incluyen stop-loss, take-profit, break-even y manejo del trailing stop.

## Datos e indicadores

- **Marco temporal de trading:** tipo de vela seleccionable por el usuario (`CandleType`, por defecto velas de 15 minutos).
- **Marco temporal de confirmación:** suscripción de marco temporal superior (`HigherCandleType`, por defecto velas de 1 hora) usado para:
  - Medias móviles ponderadas rápida/lenta.
  - Indicador de momentum con distancia absoluta del valor neutral (100).
  - Detección del retroceso cuando la vela anterior toca la WMA rápida.
- **Marco temporal MACD:** suscripción separada (`MacdCandleType`, por defecto velas de 30 días) para leer la dirección de la línea de señal del MACD.
- **Indicadores:**
  - Media Móvil Ponderada (WMA) en los marcos temporales de trading y superior.
  - Momentum (período configurable) en el marco temporal superior.
  - Moving Average Convergence Divergence (MACD) en el largo marco temporal.

## Lógica de trading

### Configuración larga

1. La WMA rápida del marco temporal superior está por encima de la WMA lenta.
2. La vela completada más reciente del marco temporal superior abrió por encima de la WMA rápida y la tocó con su mínimo (confirmación del retroceso).
3. Al menos una de las últimas tres lecturas de momentum absoluto supera `MomentumBuyThreshold`.
4. La línea principal del MACD está por encima de su línea de señal en el marco temporal MACD.
5. En el marco temporal de trading, la WMA rápida está por encima de la WMA lenta.

Cuando todas las reglas están satisfechas, la estrategia envía una orden de compra de mercado. El precio de entrada se registra para controlar los parámetros de riesgo.

### Configuración corta

1. La WMA rápida del marco temporal superior está por debajo de la WMA lenta.
2. La vela reciente abrió por debajo de la WMA rápida y la tocó con su máximo.
3. Uno de los últimos tres valores de momentum supera `MomentumSellThreshold`.
4. La línea principal del MACD está por debajo de la línea de señal.
5. La WMA rápida del marco temporal de trading está por debajo de la WMA lenta.

Se envía una orden de venta de mercado cuando las condiciones se alinean.

## Gestión de posición

- **Stop loss:** distancia `StopLossTicks` desde la entrada (convertida a precio absoluto usando el paso de precio del instrumento).
- **Take profit:** distancia `TakeProfitTicks` desde la entrada.
- **Break-even:** cuando el precio avanza `BreakEvenTriggerTicks`, el stop se mueve a la entrada más `BreakEvenOffsetTicks` en la dirección del trade si `UseBreakEven` está habilitado.
- **Trailing stop:** si `UseTrailingStop` es true, el stop sigue el precio por `TrailingStopTicks` una vez que la posición se mueve en beneficio.
- **Verificaciones de salida:** se ejecutan en cada vela del marco temporal de trading completada. Si se alcanza el stop o el objetivo, la estrategia cierra la posición completa con una orden de mercado.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `FastMaLength` | Longitud de la WMA rápida en el marco temporal de trading (por defecto 6). |
| `SlowMaLength` | Longitud de la WMA lenta en el marco temporal de trading (por defecto 85). |
| `BounceSlowLength` | Longitud de la WMA lenta en el marco temporal de confirmación (por defecto 200). |
| `MomentumLength` | Lookback del Momentum en el marco temporal superior (por defecto 14). |
| `MomentumBuyThreshold` | Mínimo |Momentum-100| para entradas largas (por defecto 0.3). |
| `MomentumSellThreshold` | Mínimo |Momentum-100| para entradas cortas (por defecto 0.3). |
| `StopLossTicks` | Distancia del stop-loss en ticks (por defecto 200). |
| `TakeProfitTicks` | Distancia del take-profit en ticks (por defecto 500). |
| `UseTrailingStop` | Habilitar lógica de trailing stop (por defecto true). |
| `TrailingStopTicks` | Distancia del trailing stop en ticks (por defecto 400). |
| `UseBreakEven` | Habilitar ajuste de break-even (por defecto true). |
| `BreakEvenTriggerTicks` | Disparador de beneficio para break-even en ticks (por defecto 300). |
| `BreakEvenOffsetTicks` | Offset añadido al stop de break-even en ticks (por defecto 300). |
| `MacdFastLength` | Período EMA rápido del MACD (por defecto 12). |
| `MacdSlowLength` | Período EMA lento del MACD (por defecto 26). |
| `MacdSignalLength` | Período EMA de señal del MACD (por defecto 9). |
| `CandleType` | Tipo de vela del marco temporal de trading. |
| `HigherCandleType` | Tipo de vela del marco temporal de confirmación. |
| `MacdCandleType` | Tipo de vela del marco temporal MACD. |

## Notas

- La estrategia espera que `Security.PriceStep` esté poblado para que los controles de riesgo basados en ticks se traduzcan correctamente a distancias de precio.
- Solo se mantiene una posición neta a la vez; las señales opuestas se ignoran hasta que la posición actual esté cerrada.
- La lógica procesa solo velas terminadas para evitar actuar sobre datos parciales.
