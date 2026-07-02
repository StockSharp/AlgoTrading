# Estrategia de ruptura de pivote de ASCV
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia ASCV Pivot Breakout es una adaptación de alto nivel StockSharp del MetaTrader 4 asesor experto "ASCV" (archivo `Avpb.mq4`). El robot original combina dos indicadores personalizados (ASCTrend1sig y BrainTrend1Sig), un filtro de desviación estándar, niveles de pivote diarios y aceleración de volumen intradiario para negociar configuraciones de continuación de ruptura dentro de una ventana de negociación restringida. Debido a que los indicadores personalizados propietarios no están disponibles en StockSharp, la conversión recrea su comportamiento a través de una combinación de promedios móviles, impulso estocástico y análisis de pivote diario, preservando al mismo tiempo las reglas de gestión de EA.

## Lógica de trading

1. **Filtro de sesión**: las operaciones solo se permiten entre las horas de inicio y finalización configuradas (hora predeterminada del corredor de 02:00 a 20:00). Los reinicios cada hora reproducen la lógica MQL que borra los indicadores de entrada cada vez que `Minute()==0`.
2. **Puerta de volatilidad**: un indicador de desviación estándar construido en el período de tiempo seleccionado debe estar por encima de un umbral configurable. Esto refleja la convocatoria `iStdDev` original que requería un mercado activo antes de considerar las entradas.
3. **Confirmación de tendencia**: un promedio móvil simple rápido y lento estima el filtro direccional que proporcionó ASCTrend/BrainTrend. Una señal larga requiere que el promedio rápido esté por encima del lento y que la vela cierre por encima del pivote diario. Los pantalones cortos esperan la configuración opuesta.
4. **Confirmación de impulso**: un oscilador estocástico garantiza que las rupturas alcistas se produzcan con un impulso `%K-%D` positivo y que las oportunidades bajistas tengan un impulso negativo. La dispersión absoluta entre `%K` y `%D` se reutiliza como un disparador de salida adaptativo al igual que EA se basó en la diferencia de las líneas estocásticas principales/de señal.
5. **Aceleración de volumen**: el volumen de la vela debe exceder el volumen de la vela anterior en el delta configurado (30 contratos predeterminados) para aproximarse al filtro `Volume[0]-Volume[1]`.
6. **Colocación de órdenes**: la estrategia utiliza órdenes de mercado (`BuyMarket`/`SellMarket`) con volumen fijo. Según el asesor experto, solo se permite una operación por dirección por hora.
7. **Paradas y objetivos**: las paradas se colocan en el soporte/resistencia del pivote más cercano (S1/S2 o R1/R2). Si esos niveles están demasiado cerca, se aplican distancias de retroceso expresadas en incrementos de precios. Los objetivos de ganancias siguen la misma jerarquía: R2/R1/Pivot para posiciones largas y S2/S1/Pivot para posiciones cortas. Una distancia de reserva emula el comportamiento de EA cuando los pivotes no estaban disponibles.
8. **Gestión dinámica**: el diferencial estocástico impulsa salidas anticipadas ante la pérdida de impulso. Un trailing stop medido en pasos de precio refleja las modificaciones progresivas del stop loss de la versión MQL.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `CandleType` | Plazo para el cálculo de indicadores y procesamiento de señales. | velas de 15 minutos |
| `StartHour` / `EndHour` | Límites horarios inclusivos de la sesión de negociación. | 2 / 20 |
| `FastMaLength` | Período del filtro de tendencia rápido SMA. | 10 |
| `SlowMaLength` | Período del filtro de tendencia lento SMA. | 40 |
| `StdDevLength` | Longitud retrospectiva del filtro de volatilidad de desviación estándar. | 10 |
| `StdDevThreshold` | Desviación estándar mínima requerida para operar. | 0.0005 |
| `VolumeDeltaThreshold` | Diferencia mínima entre el volumen de velas actual y anterior. | 30 |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | Períodos del oscilador estocástico. | 5 / 3 / 3 |
| `StochasticExitDelta` | Spread absoluto `%K-%D` que desencadena salidas de impulso. | 5 |
| `TrailingStopSteps` | Distancia del trailing stop en incrementos de precio. | 30 |
| `MinPivotDistanceSteps` | Distancia mínima (en pasos) requerida para objetivos basados en pivotes. | 50 |
| `StopFallbackSteps` | Distancia de parada cuando no hay suficiente soporte/resistencia del pivote. | 33 |
| `TakeProfitBufferSteps` | Distancia de toma de ganancias alternativa en pasos de precio. | 50 |
| `OrderVolume` | Volumen para cada orden de mercado. | 1 |

Todas las distancias se definen en los pasos del precio del instrumento para garantizar la compatibilidad con las especificaciones del intercambio.

## Notas de implementación

- La estrategia utiliza el patrón de alto nivel `SubscribeCandles().BindEx(...)`. Los indicadores **no** se agregan a `Strategy.Indicators` y coinciden con la guía de StockSharp.
- Los niveles de pivote se recalculan una vez por día de negociación utilizando el máximo, el mínimo y el cierre del día anterior. El primer día solo recopila datos y comienza a operar una vez que comienza el segundo día.
- `StartProtection()` está habilitado para proteger automáticamente contra desconexiones inesperadas, replicando la red de seguridad de EA.
- XML y comentarios en línea dentro del código C# explican la asignación de cada bloque a la lógica MQL original.
- Los valores de límite de pérdidas y obtención de ganancias se establecen a través de `SetStopLoss`/`SetTakeProfit` mediante conversiones de pasos de precios para permanecer independientes del corredor.

## Consejos de uso

1. Ejecute la estrategia en un instrumento que exponga tanto los datos de las velas como el volumen porque el filtro de aceleración de volumen es esencial.
2. Al optimizar, céntrese primero en los filtros de volatilidad (`StdDevThreshold`) y volumen (`VolumeDeltaThreshold`); el EA original era muy sensible a los mercados tranquilos.
3. Ajuste las distancias de pivote para que coincidan con el perfil de volatilidad del símbolo negociado. Para instrumentos con un tamaño de tick alto, aumente `MinPivotDistanceSteps` para evitar salidas prematuras.
4. Si el diferencial estocástico produce demasiadas salidas, amplíe `StochasticExitDelta` para que el trailing stop se convierta en la condición de salida dominante.

## Archivos

- `CS/AscvStrategy.cs` – la implementación C# de la estrategia.
- `README.md` – esta documentación.
- `README_ru.md` – traducción al ruso.
- `README_zh.md` – Traducción al chino.
