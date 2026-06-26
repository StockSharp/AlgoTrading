# Estrategia de Scalper de 15 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto **15M Scalper** de MetaTrader a la API de alto nivel de StockSharp. Recrea la lógica de
entrada multi-filtro (medias móviles ponderadas, oscilador estocástico, Parabolic SAR, momentum multitemporal y MACD mensual) y la
completa pila de salida que combina objetivos basados en dinero, stops de seguimiento, movimientos de punto de equilibrio y un
guardián de drawdown de capital. La versión de StockSharp opera en velas completadas exactamente como el EA y mantiene el código
orientado a eventos preservando los parámetros originales.

## Cómo Funciona

1. **Filtro de Tendencia** – las medias móviles *ponderadas* rápida y lenta calculadas en el marco temporal actual (por defecto 15
   minutos) deben estar alineadas con la dirección de la operación. Las medias utilizan el precio típico
   (`(High + Low + Close) / 3`) para coincidir con la entrada `PRICE_TYPICAL` de MQL.
2. **Reversión Estocástica** – un oscilador estocástico 5/3/3 se muestrea en las dos últimas velas cerradas. Las señales largas
   requieren que %K cruce de vuelta por encima de 20, mientras que los cortos requieren un cruce por debajo de 80, replicando las
   comprobaciones `Stoc1`/`Stoc2` del script.
3. **Confirmación de Parabolic SAR** – el valor de SAR de la barra completada debe estar por debajo de la apertura anterior para
   largos y por encima para cortos, reproduciendo el filtro de seguridad `SAR < Open[1]` / `SAR > Open[1]`.
4. **Momentum en Marco Temporal Superior** – un indicador de momentum de 14 períodos en el marco temporal superior configurable
   (por defecto 1 hora) debe desviarse de 100 en cualquiera de las tres últimas barras cerradas al menos en los umbrales de
   compra/venta. Esto implementa el trío `MomLevelB/MomLevelS` sin acceder directamente a los búferes de indicadores.
5. **MACD Mensual** – una serie de MACD en el flujo de velas mensual (por defecto barras de 30 días) mantiene la línea principal
   por encima de la señal para largos y por debajo para cortos. El mismo filtro MACD también impulsa la lógica de salida opcional
   que cierra posiciones cuando las líneas se cruzan en la dirección opuesta.
6. **Gestión de Órdenes** – cuando aparece una configuración opuesta, la estrategia primero cierra la posición existente, luego
   espera a la siguiente barra para abrir operaciones en la nueva dirección. El escalado de volumen sigue la regla de martingala
   del EA mediante `LotExponent` y el `IncreaseFactor` sensible a pérdidas.

## Gestión de Riesgos

- **Stop Loss / Take Profit** – las distancias se introducen en "puntos" de MetaTrader y se convierten a precios absolutos mediante
  `Security.PriceStep`. Para ticks fraccionales de FX (paso de precio < 1) la implementación multiplica el paso por 10 para
  imitar el manejo de pips del EA.
- **Punto de Equilibrio ("sin pérdida")** – una vez que el precio se mueve por `BreakEvenTriggerSteps`, el stop se mueve
  virtualmente a la entrada más el desplazamiento configurado. Si el precio retrocede a través de ese nivel, la posición se cierra
  a mercado.
- **Trailing Stop** – un trailing stop basado en velas observa el máximo más alto (para largos) o el mínimo más bajo (para
  cortos). Cuando el retroceso supera `TrailingStopSteps`, la posición se cierra, duplicando el comportamiento original de
  `OrderModify`.
- **Objetivos Monetarios** – `UseProfitTargetMoney`, `UseProfitTargetPercent` y `EnableMoneyTrailing` trabajan con P&L flotante
  medido mediante `PriceStep` × `StepPrice`. El port mantiene intacta la lógica de take-profit, objetivo porcentual y drawdown
  de seguimiento (`MoneyTrailingStop`).
- **Stop de Capital** – `UseEquityStop` rastrea el pico de (capital inicial + P&L realizado + beneficio flotante). Si el drawdown
  actual supera `TotalEquityRisk` por ciento de ese pico, cada posición se cierra, replicando `AccountEquityHigh()` y
  `TotalEquityRisk` del EA.
- **Dimensionamiento Martingala** – cada operación adicional en la misma dirección escala el volumen por `LotExponent`. Las
  pérdidas consecutivas aumentan el siguiente volumen base en `IncreaseFactor` por pérdida, proporcionando el mismo
  dimensionamiento de lote "adaptativo" que la rama `IncreaseFactor` de MQL.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Marco temporal de trabajo principal (por defecto velas de 15 minutos). |
| `MomentumCandleType` | Marco temporal superior para el filtro de momentum (por defecto velas de 1 hora). |
| `MacdCandleType` | Marco temporal para el filtro de tendencia MACD (por defecto velas de 30 días). |
| `FastMaPeriod`, `SlowMaPeriod` | Longitudes de las medias móviles ponderadas que definen el filtro de tendencia. |
| `MomentumPeriod` | Longitud del Momentum en el marco temporal superior. |
| `MomentumBuyThreshold`, `MomentumSellThreshold` | Desviación absoluta mínima de 100 requerida para permitir operaciones largas/cortas. |
| `StopLossSteps`, `TakeProfitSteps` | Distancias de stop protector y objetivo en pasos de precio. Poner a cero para deshabilitar. |
| `TrailingStopSteps` | Distancia del trailing stop en pasos de precio. |
| `UseMoveToBreakeven`, `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Indicador de activación de punto de equilibrio, distancia de activación y desplazamiento. |
| `UseProfitTargetMoney`, `ProfitTargetMoney` | Habilitar y configurar el objetivo de beneficio flotante basado en dinero. |
| `UseProfitTargetPercent`, `ProfitTargetPercent` | Habilitar y configurar el objetivo de beneficio flotante basado en porcentaje. |
| `EnableMoneyTrailing`, `MoneyTrailingTakeProfit`, `MoneyTrailingStop` | Activación del trailing monetario y máximo retroceso permitido en divisa de cuenta. |
| `UseEquityStop`, `TotalEquityRisk` | Habilitar el control de drawdown de capital y establecer el porcentaje permitido del capital máximo. |
| `BaseVolume`, `LotExponent`, `IncreaseFactor`, `MaxTrades` | Opciones de dimensionamiento martingala: lote inicial, multiplicador, incremento basado en pérdidas y máximo de adiciones. |
| `UseExitByMacd` | Cerrar posiciones cuando la línea principal de MACD cruza la señal en contra de la operación. |

## Uso

1. Adjunte la estrategia a un valor y asegúrese de que `Security.PriceStep` y `Security.StepPrice` estén rellenos. Estos valores
   se utilizan para traducir las entradas basadas en pips y los objetivos monetarios a números absolutos.
2. Ajuste `CandleType`, `MomentumCandleType` y `MacdCandleType` si desea ejecutar el scalper en diferentes marcos temporales. Los
   valores predeterminados replican la configuración original de 15 minutos / 1 hora / mensual.
3. Ajuste las distancias basadas en pips (`StopLossSteps`, `TakeProfitSteps`, `TrailingStopSteps`, configuración de punto de
   equilibrio) para adaptarse al tamaño de tick del instrumento. Comience con los valores predeterminados proporcionados y
   auméntelos para mercados más volátiles.
4. Establezca las preferencias de gestión de dinero: decida si usar take profits monetarios o porcentuales, active el trailing
   monetario y configure el stop de capital si desea una red de seguridad contra drawdowns profundos.
5. Lance la estrategia. Se suscribirá automáticamente a todos los flujos de velas requeridos, trazará indicadores (si hay un
   gráfico disponible) y comenzará a evaluar señales una vez que cada indicador tenga suficiente historial.

## Notas y Diferencias con el EA Original

- El port utiliza el modelo de posición agregada de StockSharp. Cuando aparece una señal opuesta, la posición actual se cierra
  primero y la nueva dirección se evalúa en la siguiente vela, manteniendo el comportamiento determinista.
- Los cálculos basados en dinero dependen de `Security.PriceStep` y `Security.StepPrice`. Si el lugar no proporciona estos
  valores, los objetivos monetarios se omiten (el beneficio flotante se reporta como cero), exactamente como se indica en los
  comentarios del código.
- `IncreaseFactor` agrega `IncreaseFactor × pérdidas_consecutivas` al siguiente volumen base en lugar de usar margen libre (que
  no está disponible en el entorno sandbox). Esto sigue capturando la intención de aumentar el tamaño después de rachas de
  pérdidas.
- Todas las decisiones se toman en velas terminadas para evitar el doble conteo de señales, coincidiendo con las comprobaciones
  barra por barra de la implementación de MetaTrader.
- La estrategia dibuja los mismos indicadores en el gráfico cuando hay un visualizador disponible, ayudando en la depuración y
  haciendo que el port sea fácil de comparar con el EA.

Revise cuidadosamente el tamaño de tick, el precio del paso y las restricciones de volumen de su bróker antes del trading en
vivo. Estos valores impactan directamente en cómo las distancias basadas en pips y los objetivos monetarios se convierten dentro
de la estrategia.
