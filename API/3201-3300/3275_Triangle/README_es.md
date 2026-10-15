# Estrategia de triángulo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia adapta el asesor experto de MetaTrader **Triangle v1** a la API de alto nivel de StockSharp. El EA original combinaba filtros de media móvil ponderada en un marco temporal superior, una comprobación de divergencia de momentum y una confirmación MACD de muy largo plazo antes de colocar órdenes de estilo ruptura. La versión StockSharp conserva la lógica multitemporal y sustituye la gestión monetaria tick a tick por órdenes de protección basadas en velas.

## Funcionamiento

1. **Filtros multitemporales.** El marco temporal de trabajo (`CandleType`, predeterminado 15 minutos) se usa para ejecutar operaciones. Los filtros de tendencia y momentum se calculan en un marco temporal superior (`TrendCandleType`, predeterminado 1 hora) para reflejar las llamadas MQL que referenciaban `T`.
2. **Puerta de tendencia LWMA.** Las medias móviles ponderadas rápida y lenta (equivalente LWMA) deben estar alineadas. Las configuraciones largas exigen que la LWMA rápida permanezca por encima de la LWMA lenta; los cortos exigen la relación opuesta.
3. **Desviación de momentum.** Una serie de momentum de 14 períodos en el marco temporal superior debe desviarse del nivel neutral (100) al menos `MomentumThreshold` en cualquiera de las últimas tres velas completadas, reproduciendo las comprobaciones `MomLevelB/MomLevelS`.
4. **Confirmación MACD.** Un marco temporal muy alto (`MacdCandleType`, predeterminado velas de 30 días ≈ mensual) debe mostrar la línea principal MACD en el lado correcto de la línea de señal antes de permitir operaciones, copiando la condición `MacdMAIN0` frente a `MacdSIGNAL0`.
5. **Salidas de protección.** Las distancias de stop loss y take profit se configuran en pasos de precio. Cuando cualquiera de los niveles se alcanza en una barra completada, la estrategia cierra la posición con una orden de mercado.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `FastMaPeriod`, `SlowMaPeriod` | Longitudes de las medias móviles ponderadas del marco temporal superior. |
| `MomentumPeriod` | Período del filtro de momentum en el marco temporal superior. |
| `MomentumThreshold` | Desviación absoluta mínima desde 100 requerida en cualquiera de las últimas tres lecturas de momentum. Establecer en 0 para desactivar el filtro. |
| `MacdFastLength`, `MacdSlowLength`, `MacdSignalLength` | Parámetros MACD aplicados a `MacdCandleType`. |
| `StopLossSteps`, `TakeProfitSteps` | Stop de protección y distancias de objetivo medidas en pasos de precio del instrumento (ticks). Usar 0 para desactivar. |
| `CandleType` | Marco temporal de trading usado para la ejecución de órdenes. |
| `TrendCandleType` | Marco temporal superior que alimenta las LWMA y el momentum. |
| `MacdCandleType` | Marco temporal usado para el filtro de confirmación MACD. |

## Uso

1. Seleccione un instrumento y configure `CandleType`, `TrendCandleType` y `MacdCandleType` para que coincidan con los marcos temporales que desea analizar.
2. Ajuste las longitudes de MA, momentum y MACD si quiere adaptar el sistema a otro mercado o régimen de volatilidad.
3. Establezca `StopLossSteps` y `TakeProfitSteps` según el tamaño de tick del instrumento. La estrategia convierte automáticamente los conteos de pasos en distancias reales de precio.
4. Inicie la estrategia. Se suscribe a todos los flujos de velas necesarios, actualiza indicadores con la API de alto nivel `Bind` y gestiona la posición cuando se alcanzan stops u objetivos.

## Diferencias con el EA original

- Las salidas basadas en dinero (`Use_TP_In_Money`, `Use_TP_In_percent`) y el bloque de protección de balance no se recrean porque StockSharp trabaja en unidades del instrumento. Se puede lograr un comportamiento equivalente ajustando `StopLossSteps`/`TakeProfitSteps`.
- La lógica de trailing-stop, break-even y equity-stop del EA dependía del procesamiento de ticks y de llamadas de modificación de órdenes específicas de MetaTrader. La adaptación conserva el enfoque más simple de stop fijo por claridad; los usuarios pueden ampliar `UpdatePositionState` con reglas trailing si lo desean.
- Las líneas de tendencia manuales (`TREND`/`TRENDLOW`) y los arreglos de fractales se usaban como filtros discrecionales en el EA. Se omiten intencionalmente para que la estrategia StockSharp siga siendo completamente sistemática.
- La estrategia mantiene siempre como máximo una posición neta, lo que coincide con el uso típico aunque el EA exponía un parámetro `Max_Trades`.

Ajuste los umbrales y parámetros de marco temporal al instrumento que opera. Los mercados volátiles suelen requerir valores más amplios para evitar quedar filtrados por pequeñas fluctuaciones de momentum.
