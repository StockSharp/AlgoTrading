# Estrategia de Scalper de Un Minuto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto **1 MINUTE SCALPER** de MetaTrader 4 a la API de alto nivel de StockSharp. Mantiene la
confirmación de tendencia multicapa, el momentum multi-temporal y el filtro MACD a largo plazo del robot original mientras adapta
los controles de riesgo al modelo centrado en posiciones de StockSharp.

## Lógica Central

1. **Pila de Tendencia** – trece medias móviles linealmente ponderadas (LWMA 3/5/8/10/12/15/30/35/40/45/50/55/200) deben
   alinearse en orden estricto. Las operaciones largas requieren que cada media más corta imprima por encima de la siguiente,
   mientras que los cortos invierten la condición.
2. **Puerta de Tendencia Principal** – una LWMA rápida adicional (por defecto 6) debe mantenerse por encima de la LWMA lenta
   (por defecto 85) para largos y por debajo para cortos, reflejando la comprobación rápido-vs-lento del EA.
3. **Estructura de Vela** – las entradas solo se activan cuando los patrones de superposición del script están presentes: para
   largos el mínimo de dos barras atrás debe estar por debajo del máximo anterior; para cortos el mínimo anterior debe caer bajo
   el máximo de dos barras atrás.
4. **Filtro de Momentum** – un indicador de momentum de 14 períodos calculado en un marco temporal superior (por defecto velas
   de 15 minutos) debe desviarse de 100 al menos en los umbrales configurados en cualquiera de los últimos tres valores. Esto
   reproduce las comparaciones `MomLevelB/MomLevelS`.
5. **Sesgo MACD Mensual** – un MACD construido en el marco temporal MACD seleccionado (por defecto velas de 30 días como proxy
   de datos mensuales) debe mostrar la línea principal por encima de la señal para largos o por debajo para cortos.

## Gestión de Operaciones

- **Protección Inicial** – las distancias de stop-loss y take-profit se expresan en pasos del instrumento (puntos). Cuando
  abre una posición, la estrategia convierte estos recuentos de pasos a precios absolutos usando `Security.PriceStep`.
- **Movimiento de Punto de Equilibrio** – después de que el precio se mueve `BreakEvenTriggerSteps` a favor, el stop se mueve
  a la entrada más `BreakEvenOffsetSteps` (para cortos se aplica la lógica espejada). El indicador se activa una vez por posición.
- **Trailing por Pasos** – cuando `TrailingStopSteps` es positivo, el stop sigue el precio más alto (o más bajo) desde la
  entrada en el número especificado de pasos.
- **Trailing Monetario** – una vez que el beneficio flotante supera `MoneyTrailTarget` (divisa), la estrategia rastrea el PnL
  flotante máximo y cierra la posición si el retroceso es igual a `MoneyTrailStop`.
- **Objetivos Monetarios/Porcentuales** – objetivos de take-profit absolutos u opcionales cierran toda la exposición cuando el
  PnL flotante cruza los umbrales configurados. El objetivo porcentual usa el valor inicial de la cartera capturado cuando
  comienza la estrategia.
- **Stop de Capital** – la estrategia monitoriza el capital máximo (valor de cartera más PnL abierto). Si el drawdown desde ese
  pico supera `EquityRiskPercent`, todas las posiciones se cierran, replicando la salvaguarda `AccountEquityHigh()` del EA.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `Volume` | Volumen de orden para nuevas entradas. Se añade a la posición actual absoluta para que las reversiones cambien la exposición inmediatamente. |
| `FastMaPeriod` / `SlowMaPeriod` | Longitudes de LWMA para el filtro de tendencia principal. |
| `MomentumPeriod` | Longitud del indicador de momentum en marco temporal superior. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Desviación absoluta mínima de 100 requerida para la confirmación de momentum largo/corto. |
| `MacdFastLength` / `MacdSlowLength` / `MacdSignalLength` | Configuración de MACD aplicada a `MacdCandleType`. |
| `StopLossSteps` / `TakeProfitSteps` | Distancias de stop protector y objetivo medidas en pasos de precio. Poner en cero para deshabilitar. |
| `TrailingStopSteps` | Distancia del trailing stop basado en pasos (0 deshabilita). |
| `BreakEvenTriggerSteps` / `BreakEvenOffsetSteps` | Distancia para activar el movimiento de punto de equilibrio y el offset aplicado al mover el stop. |
| `UseMoneyTakeProfit`, `MoneyTakeProfit` | Habilitar y dimensionar el objetivo de beneficio flotante basado en divisa. |
| `UsePercentTakeProfit`, `PercentTakeProfit` | Habilitar y dimensionar el objetivo de beneficio flotante como porcentaje del capital inicial. |
| `EnableMoneyTrailing`, `MoneyTrailTarget`, `MoneyTrailStop` | Configurar la lógica de trailing del beneficio flotante. |
| `UseEquityStop`, `EquityRiskPercent` | Habilitar el stop de drawdown y definir el porcentaje máximo de drawdown. |
| `CandleType` | Velas de trabajo principales (por defecto 1 minuto). |
| `MomentumCandleType` | Velas de marco temporal superior para el indicador de momentum (por defecto 15 minutos). |
| `MacdCandleType` | Velas usadas para el filtro de tendencia MACD (por defecto 30 días ≈ mensual). |

## Diferencias vs. el Experto de MT4

- StockSharp utiliza posiciones netas, por lo que la estrategia siempre mantiene una única posición agregada en lugar de
  múltiples tickets hasta `Max_Trades`. Las reversiones cierran la exposición existente antes de abrir en la dirección opuesta.
- `PercentTakeProfit` hace referencia al valor de cartera capturado al inicio en lugar del `AccountBalance()` en constante
  cambio usado por MetaTrader, lo que evita objetivos ruidosos cuando las operaciones externas modifican el saldo.
- La lógica de salida basada en dinero (`Take_Profit_In_Money` y `TRAIL_PROFIT_IN_MONEY2`) opera sobre el PnL flotante en vivo
  calculado desde el precio de entrada promedio de la estrategia. Esto coincide con el comportamiento del EA pero dentro del
  marco de protección de StockSharp.
- La plataforma debe suministrar feeds de velas para los marcos temporales seleccionados (`CandleType`, `MomentumCandleType`,
  `MacdCandleType`). Asegúrese de que los adaptadores que utiliza admitan las resoluciones solicitadas.

Ajuste los umbrales para adaptarse a su instrumento y sesión. Los pares con spreads estrechos o muy volátiles pueden requerir
distancias de pasos más amplias o umbrales de momentum más grandes para reducir el ruido.
