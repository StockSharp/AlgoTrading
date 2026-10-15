# Estrategia LotScalp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia abre una sola operación por día a una hora especificada basándose en la diferencia entre las aperturas de velas pasadas.

## Cómo Funciona

1. **Esperar el Momento de Operar**: La estrategia monitorea los tiempos de apertura de velas. Una vez que la hora es mayor que `TradeTime`, el trading se permite en la próxima ocurrencia de esa hora.
2. **Generación de Señales**:
   - Cuando la hora actual es igual a `TradeTime`, la estrategia compara el precio de apertura de hace `t1` barras con el precio de apertura de hace `t2` barras.
   - Si la diferencia `Open[t1] - Open[t2]` supera `DeltaShort` puntos, se abre una posición corta.
   - Si la diferencia `Open[t2] - Open[t1]` supera `DeltaLong` puntos, se abre una posición larga.
3. **Gestión de Posiciones**:
   - Para posiciones largas, la estrategia sale cuando el precio alcanza `TakeProfitLong` por encima de la entrada o `StopLossLong` por debajo.
   - Para posiciones cortas, sale cuando el precio se mueve `TakeProfitShort` por debajo o `StopLossShort` por encima de la entrada.
   - Las posiciones también se cierran si permanecen abiertas más de `MaxOpenTime` horas.

La estrategia opera con un volumen fijo y no entra en nuevas operaciones hasta el día siguiente.

## Parámetros

| Nombre | Descripción |
| ------ | ----------- |
| `CandleType` | Fuente de velas para la estrategia. |
| `Volume` | Volumen de la orden. |
| `TakeProfitLong` | Toma de ganancias en puntos para operaciones largas. |
| `StopLossLong` | Stop loss en puntos para operaciones largas. |
| `TakeProfitShort` | Toma de ganancias en puntos para operaciones cortas. |
| `StopLossShort` | Stop loss en puntos para operaciones cortas. |
| `TradeTime` | Hora del día en que se evalúan las señales. |
| `T1` | Número de barras hacia atrás para el primer precio de apertura. |
| `T2` | Número de barras hacia atrás para el segundo precio de apertura. |
| `DeltaLong` | Diferencia mínima (en puntos) entre `Open[t2]` y `Open[t1]` para abrir una operación larga. |
| `DeltaShort` | Diferencia mínima (en puntos) entre `Open[t1]` y `Open[t2]` para abrir una operación corta. |
| `MaxOpenTime` | Tiempo máximo de mantenimiento en horas. |

## Notas

- Solo se procesan velas completadas.
- La estrategia usa el paso de precio del instrumento para convertir umbrales basados en puntos en precios absolutos.
- No se utilizan indicadores adicionales.
