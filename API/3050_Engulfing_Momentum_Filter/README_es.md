# Estrategia de Filtro de Momentum Engulfing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto **ENGULFING** de MetaTrader a la API de alto nivel de StockSharp. Combina un patrón de vela envolvente alcista/bajista en el marco temporal de trabajo con confirmación de momentum de marco temporal superior y un filtro de tendencia MACD mensual. La gestión de riesgo reproduce el comportamiento original de break-even y trailing usando distancias de stop medidas en pasos del instrumento.

## Cómo funciona

1. **Patrón de velas** – la última vela terminada debe engullir la barra anterior en la dirección del trade. La estrategia también comprueba que la barra de hace dos períodos se superponga a la barra anterior, reflejando la confirmación basada en fractales del original.
2. **Filtro de tendencia** – las medias móviles *ponderadas* rápida y lenta (análogo de LWMA) controlan las entradas. Las operaciones largas requieren que la media rápida esté por encima de la lenta y viceversa para los cortos.
3. **Filtro de momentum** – un indicador de momentum de 14 períodos calculado en un marco temporal superior debe desviarse del nivel neutral (100) al menos el umbral configurado en cualquiera de los últimos tres valores. Esto reproduce las comprobaciones `MomLevelB/MomLevelS` del código MQL.
4. **Filtro MACD** – una serie MACD mensual (30 días) debe mostrar la línea principal por encima de la línea de señal para los largos y por debajo para los cortos, igual que la comparación `MacdMAIN0` vs `MacdSIGNAL0` en el EA.
5. **Gestión de órdenes** – la estrategia siempre gira la posición cuando aparece una señal opuesta. La lógica de protección cierra las operaciones siempre que se activen las reglas de stop, objetivo, break-even o trailing.

## Gestión de riesgo

- **Stop Loss / Take Profit** – las distancias se configuran en pasos del instrumento (ticks). Reflejan las entradas `Stop_Loss` y `Take_Profit` del EA original.
- **Trailing Stop** – trailing opcional medido en pasos. El stop sigue el mejor precio alcanzado después de la entrada.
- **Movimiento Break-Even** – una vez que el precio avanza `BreakEvenTriggerSteps`, el stop se mueve a la entrada más `BreakEvenOffsetSteps`, reproduciendo la función "sin pérdida" (`USEMOVETOBREAKEVEN`).

Los objetivos basados en dinero del script MQL (`Use_TP_In_Money`, `Take_Profit_In_percent`) se omiten intencionalmente para mantener la lógica consistente con el sistema de unidades de StockSharp. Los exits basados en porcentaje o moneda pueden recrearse ajustando los parámetros de stop/take.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `FastMaPeriod` / `SlowMaPeriod` | Longitudes de las medias móviles ponderadas usadas para la confirmación de tendencia. |
| `MomentumPeriod` | Longitud del Momentum en el marco temporal superior. |
| `MomentumBuyThreshold` / `MomentumSellThreshold` | Desviación absoluta mínima de 100 requerida para el filtro de momentum. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Configuración MACD aplicada a `MacdCandleType`. |
| `StopLossSteps`, `TakeProfitSteps` | Distancias de stop protector y objetivo en pasos de precio. Establecer en cero para deshabilitar. |
| `TrailingStopSteps` | Distancia opcional del trailing stop (0 deshabilita el trailing). |
| `BreakEvenTriggerSteps`, `BreakEvenOffsetSteps` | Distancia requerida antes de mover el stop a break-even y el offset aplicado. |
| `CandleType` | Marco temporal principal donde se evalúan los patrones envolventes. |
| `HigherCandleType` | Marco temporal superior usado para el filtro de momentum (por defecto 1 hora). |
| `MacdCandleType` | Marco temporal para el filtro de tendencia MACD (por defecto 30 días ≈ mensual). |

## Uso

1. Adjuntar la estrategia a un instrumento y establecer `CandleType`, `HigherCandleType` y `MacdCandleType` para que coincidan con los marcos temporales preferidos.
2. Ajustar los parámetros de MA y momentum si deseas alinear con una estructura de mercado diferente.
3. Configurar las distancias de stop, take profit, trailing y break-even en pasos de precio que correspondan al tamaño de tick de tu instrumento.
4. Iniciar la estrategia; se suscribirá automáticamente a todos los feeds de velas necesarios y comenzará a evaluar señales una vez que los indicadores se formen.

## Notas y diferencias con el EA original

- Las medias móviles ponderadas replican los cálculos LWMA usados en MQL sin iterar manualmente sobre los precios.
- La lógica de break-even y trailing se aplica en velas completadas, coincidiendo con el enfoque barra por barra del EA mientras aprovecha los helpers de protección de StockSharp.
- El trailing basado en dinero y los exits basados en porcentaje no se portan porque StockSharp opera en unidades del instrumento; un comportamiento equivalente puede lograrse calibrando los parámetros basados en pasos.
- La estrategia asume una posición a la vez, que es el escenario de uso común del EA fuente aunque exponía una entrada `Max_Trades`.

Ajusta los umbrales y marcos temporales para que coincidan con el activo que estás operando. Los instrumentos de mayor volatilidad a menudo requieren distancias de pasos más grandes o umbrales de momentum más amplios para evitar salidas prematuras.
