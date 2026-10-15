# Estrategia de Heikin Ashi Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el experto de MetaTrader 4 "Heikin Ashi Trader" a StockSharp. Mantiene la lógica de confirmación multi-indicador del robot original y la implementa con la API de suscripción de velas de alto nivel para que cada decisión se base únicamente en barras terminadas.

## Detalles
- **Indicadores**:
  - Velas Heikin-Ashi calculadas desde el marco temporal de trabajo.
  - Dos medias móviles ponderadas linealmente (LWMA) usando el precio típico de la vela (`(high + low + close) / 3`).
  - Un oscilador estocástico (los períodos `%K/%D/Smooth` son configurables por el usuario).
  - Momentum (distancia del nivel neutral 100).
  - Convergencia/Divergencia de Medias Móviles (MACD).
- **Criterios de entrada**:
  - **Largo**: La última vela Heikin-Ashi debe ser alcista, al menos uno de los últimos tres valores estocásticos debe estar por encima del nivel de sobrecompra, la LWMA rápida debe estar por encima de la LWMA lenta, la distancia de momentum desde 100 debe superar el umbral de compra, y la línea MACD debe estar por encima de su señal.
  - **Corto**: Condiciones espejo — vela Heikin-Ashi bajista, estocástico por debajo del nivel de sobreventa, LWMA rápida por debajo de la LWMA lenta, distancia de momentum por encima del umbral de venta, y línea MACD por debajo de su señal.
  - Opcionalmente aplanar la exposición opuesta antes de tomar el nuevo trade (`CloseOppositePositions`).
- **Gestión de posiciones**:
  - Stop-loss y take-profit fijos en pips (derivados del paso de precio del valor).
  - Trailing stop opcional que sigue el cierre una vez que el trade avanza `TrailingStopPips`.
  - Lógica de break-even que mueve el stop a `Entry ± BreakEvenOffsetPips` después de que el precio avanza `BreakEvenTriggerPips` a favor de la posición.
  - Interruptor de eliminación manual (`ForceExit`) para aplanar todo en el siguiente vela.
- **Diferencias vs. la versión MT4**:
  - El EA original evaluaba el momentum en un marco temporal superior. Este puerto mantiene los mismos períodos de indicadores pero los lee desde el flujo de velas primario para permanecer dentro de la API de alto nivel de StockSharp. Los parámetros permiten ajustar los umbrales si desea recrear la sensibilidad original.
  - Las reglas de stop basadas en dinero del código MT4 no están incluidas. El riesgo se maneja a través de stops basados en precio y el módulo de break-even.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `CandleType` | Marco temporal (o cualquier otro tipo de vela) utilizado para todos los indicadores y decisiones de trading. |
| `FastMaPeriod`, `SlowMaPeriod` | Períodos de las medias móviles ponderadas linealmente rápida y lenta (precio típico). |
| `StochasticKPeriod`, `StochasticDPeriod`, `StochasticSlowing` | Longitudes `%K/%D` y factor de suavizado del oscilador estocástico. |
| `StochasticOverbought`, `StochasticOversold` | Umbrales estocásticos que deben cruzarse durante los últimos tres valores completados. |
| `MomentumPeriod` | Longitud del indicador Momentum. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Distancia absoluta mínima desde la línea 100 requerida para trades largos/cortos. |
| `MacdFastPeriod`, `MacdSlowPeriod`, `MacdSignalPeriod` | Configuración MACD. |
| `CloseOppositePositions` | Cerrar el lado opuesto antes de entrar en un nuevo trade. |
| `MaxPositions` | Exposición neta máxima por dirección (`0` = ilimitado). |
| `TradeVolume` | Volumen de cada nueva orden; también asignado al `Volume` de la estrategia. |
| `UseStopLoss`, `StopLossPips` | Habilitar y dimensionar el stop protector en pips. |
| `UseTakeProfit`, `TakeProfitPips` | Habilitar y dimensionar el take-profit en pips. |
| `UseTrailingStop`, `TrailingStopPips` | Habilitar la lógica de trailing stop y definir su distancia. |
| `UseBreakEven`, `BreakEvenTriggerPips`, `BreakEvenOffsetPips` | Distancia de activación del break-even y el offset bloqueado. |
| `ForceExit` | Cuando `true`, todas las posiciones se cierran en el siguiente vela procesado. |

## Notas de implementación
- La estrategia se suscribe a velas mediante `SubscribeCandles().BindEx(...)` para que los indicadores reciban valores terminados y el código nunca llame a `GetValue()` directamente.
- La conversión de pips usa el `PriceStep` del instrumento; si su mercado cotiza pips fraccionarios, configure el paso del valor apropiadamente.
- Las actualizaciones de trailing y break-even solo mueven el stop en la dirección favorable. La lógica de restablecimiento borra los valores de stop/objetivo almacenados en caché cada vez que se cierra un trade para que las nuevas posiciones comiencen con configuraciones de riesgo frescas.
