# Estrategia Crypto Analysis
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Esta estrategia es un port para StockSharp del asesor experto de MetaTrader 4 "Crypto Analysis". Busca rupturas que aparecen después de que el precio toque la banda exterior de Bollinger en el marco principal de negociación, mientras la estructura del mercado permanece bajista (LWMA rápida por debajo de la LWMA lenta). El sistema solo permite operaciones cuando una ráfaga de momentum en un marco superior y un filtro MACD mensual coinciden con la dirección deseada. Una vez dentro del mercado, la posición se gestiona mediante un bloque de protección por capas que replica el EA original: stops basados en pips, trailing basado en dinero, reubicación a break-even y controles de drawdown de cartera.

## Lógica de negociación
- **Marco de señal:** configurable (M15 por defecto). Todas las reglas de entrada/salida se evalúan en estas velas.
- **Disparador de volatilidad:** el mínimo de la vela anterior debe tocar o perforar la banda inferior de Bollinger (20, 2) para preparar una configuración larga; tocar la banda superior prepara una configuración corta.
- **Filtro de tendencia:** ambos escenarios requieren que la media móvil ponderada lineal rápida (LWMA, 6 por defecto) permanezca por debajo de la LWMA lenta (85 por defecto), replicando el sesgo bajista del EA.
- **Confirmación RSI:** RSI(14) debe estar por encima de 50 para largos y por debajo de 50 para cortos.
- **Ráfaga de momentum:** la desviación absoluta máxima de los tres últimos valores Momentum(14) del marco superior frente a la línea base 100 debe superar los umbrales de compra/venta. Esto captura los picos de momentum usados por el código MQL.
- **Filtro MACD mensual:** un MACD separado mensual (velas de 30 días por defecto) (12, 26, 9) confirma la dirección; los largos requieren MACD principal sobre señal, los cortos exigen lo contrario.
- **Ejecución de entrada:** cuando todos los filtros se alinean, la estrategia abre una orden de mercado. Las posiciones opuestas se aplanan antes de revertir para mantener una sola posición neta, lo que refleja el comportamiento del EA al cerrar operaciones contrarias.

## Gestión de posición
- **Stop y objetivo iniciales:** las distancias configurables en pips se convierten desde el tick del instrumento con el mismo manejo de 5 dígitos/3 dígitos que el EA (los pasos `0.00001` y `0.001` se multiplican por 10).
- **Trailing stop:** después de formar un nuevo máximo (largo) o mínimo (corto), el stop se arrastra detrás del precio por `TrailingStopPips`, respetando siempre el mejor nivel alcanzado.
- **Break-even:** cuando el beneficio alcanza `BreakEvenTriggerPips`, el stop se mueve al precio de entrada más `BreakEvenOffsetPips` (largo) o menos el desplazamiento (corto).
- **Objetivos monetarios:** topes opcionales de beneficio absoluto o porcentual cierran la posición tan pronto como el PnL flotante alcanza el nivel solicitado.
- **Trailing monetario:** cuando el beneficio no realizado supera `MoneyTrailTarget`, la estrategia sigue el máximo y cierra la posición si la devolución alcanza o supera `MoneyTrailStop`.
- **Stop de patrimonio:** se controla el patrimonio flotante (valor actual de cartera más PnL no realizado); si el drawdown desde el máximo supera `EquityRiskPercent`, la posición se aplana.

## Datos multimarco
Se registran automáticamente tres suscripciones:
1. Serie principal de velas para las reglas Bollinger/LWMA/RSI.
2. Velas de marco superior para el filtro de momentum (H1 por defecto).
3. Velas mensuales para la confirmación MACD (barras de 30 días por defecto).

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `OrderVolume` | Tamaño base de orden. Las posiciones opuestas se cierran antes de abrir una nueva. |
| `UseMoneyTakeProfit` | Activa el objetivo de take-profit monetario absoluto. |
| `MoneyTakeProfit` | Beneficio en la moneda de la cartera que dispara una salida cuando `UseMoneyTakeProfit` es verdadero. |
| `UsePercentTakeProfit` | Activa el objetivo de take-profit porcentual calculado desde el patrimonio inicial. |
| `PercentTakeProfit` | Porcentaje de beneficio requerido para cerrar la posición cuando el objetivo porcentual está activo. |
| `EnableMoneyTrailing` | Activa el bloque de trailing basado en dinero. |
| `MoneyTrailTarget` | Nivel de beneficio que inicia el trailing monetario. |
| `MoneyTrailStop` | Devolución máxima permitida de beneficio después de alcanzar `MoneyTrailTarget`. |
| `StopLossPips` | Distancia inicial de stop-loss en pips. |
| `TakeProfitPips` | Distancia inicial de take-profit en pips. |
| `TrailingStopPips` | Distancia del trailing stop en pips. |
| `UseBreakEven` | Activa la reubicación del stop a break-even. |
| `BreakEvenTriggerPips` | Beneficio en pips necesario antes de activar la protección break-even. |
| `BreakEvenOffsetPips` | Pips adicionales añadidos al precio de entrada al colocar el stop de break-even. |
| `FastMaPeriod` | Longitud de la LWMA rápida calculada sobre precio típico. |
| `SlowMaPeriod` | Longitud de la LWMA lenta calculada sobre precio típico. |
| `MomentumPeriod` | Periodo del indicador Momentum en el marco superior. |
| `MomentumBuyThreshold` | Desviación mínima de momentum para entradas largas. |
| `MomentumSellThreshold` | Desviación mínima de momentum para entradas cortas. |
| `MacdFastLength` | Longitud de EMA rápida para el filtro MACD de marco superior. |
| `MacdSlowLength` | Longitud de EMA lenta para el filtro MACD de marco superior. |
| `MacdSignalLength` | Longitud de señal para el filtro MACD de marco superior. |
| `UseEquityStop` | Activa la protección contra drawdown de cartera. |
| `EquityRiskPercent` | Porcentaje permitido de drawdown de patrimonio antes de cerrar la posición por fuerza. |
| `CandleType` | Marco principal usado para entradas. |
| `MomentumCandleType` | Marco superior usado para confirmación de momentum. |
| `MacdCandleType` | Marco superior usado para confirmación MACD. |

## Notas
- El port StockSharp mantiene una sola posición neta, igual que el EA que cierra órdenes opuestas antes de abrir una nueva operación.
- Todas las reglas de protección operan sobre velas cerradas para replicar el procesamiento de "nueva barra" del script original.
- Al usar símbolos sintéticos o instrumentos sin tamaño de pip estándar, ajuste `StopLossPips` y parámetros relacionados al valor de tick de la bolsa.
