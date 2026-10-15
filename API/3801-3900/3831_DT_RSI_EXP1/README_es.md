# DT RSI Estrategia EXP1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este puerto replica el asesor experto MT4 **DT-RSI-EXP1**. La estrategia escanea oscilaciones de RSI de 15 minutos para detectar dobles techos o dobles suelos alrededor de los niveles 60/40. Se realiza una operación larga cuando los picos recientes RSI retroceden sin imprimir ningún mínimo por debajo de 40, mientras que el filtro de tendencia de 4 horas apunta hacia abajo. Los cortos reflejan la lógica con mínimos por encima de 60 y un filtro de tendencia ascendente. Se adjuntan un stop loss fijo y una toma de ganancias a cada posición, y un stop dinámico opcional protege las ganancias. Las posiciones se cierran a la fuerza cuando RSI se extiende a niveles extremos de 70/30, copiando el comportamiento de salida original.

## Detalles

- **Criterios de entrada**:
  - **Largo**: dos picos alcistas RSI con el segundo por encima de 60, sin mínimos bajistas por debajo de 40 en el medio, EMA de 4 horas por debajo del cierre anterior, RSI(1) cruzando por encima del escote proyectado, RSI(2) todavía por debajo de él, RSI(2) < 50 y RSI(0) < 55.
  - **Corto**: dos mínimos bajistas RSI con el segundo por debajo de 40, sin picos alcistas por encima de 60 en el medio, EMA de 4 horas por encima del cierre anterior, RSI(1) cruzando por debajo de la línea de escote proyectada, RSI(2) > 50 y RSI(0) > 47.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - RSI extremos (RSI > 70 para largos, RSI < 30 para cortos).
  - Objetivos de stop-loss/take-profit calculados a partir de los incrementos de precios.
  - Trailing stop opcional que bloquea las ganancias una vez que el precio se mueve en `TrailingStopPoints`.
- **Paradas**: Stop-loss fijo y take-profit, trailing stop opcional.
- **Valores predeterminados**:
  - `CandleType` = velas de 15 minutos.
  - `TrendCandleType` = velas de 240 minutos (filtro de tendencia EMA).
  - `RsiPeriod` = 47.
  - `StopLossPoints` = 26.
  - `TakeProfitPoints` = 76.
  - `TrailingStopPoints` = 0 (deshabilitado).
- **Filtros**:
  - Categoría: Entradas de seguimiento de tendencias en estructuras RSI.
  - Dirección: Ambos.
  - Indicadores: RSI, EMA filtro de tendencias.
  - Paradas: Sí.
  - Complejidad: Intermedia (detección de swing con múltiples restricciones).
  - Plazo: Intradiario (M15 con filtro H4).
  - Estacionalidad: No.
  - Redes neuronales: no.
  - Divergencia: No.
  - Nivel de riesgo: Medio.

## Parámetros

| Nombre | Predeterminado | Descripción | Optimizable |
| ---- | ------- | ----------- | ----------- |
| `CandleType` | 15 minutos | Serie de velas primaria utilizada para calcular RSI y señales. | si |
| `TrendCandleType` | 240 minutos | Marco de tiempo más alto utilizado por el filtro de tendencia EMA (reemplazo del indicador MT4 RFTL). | si |
| `RsiPeriod` | 47 | RSI longitud aplicada a las velas primarias. | si |
| `StopLossPoints` | 26 | Distancia al stop-loss en pasos de precio. | si |
| `TakeProfitPoints` | 76 | Distancia a la toma de ganancias en pasos de precio. | si |
| `TrailingStopPoints` | 0 | Compensación del trailing-stop en incrementos de precios (`0` desactiva el trailing). | si |

## Notas

- El indicador personalizado MetaTrader `RFTL` se aproxima con un período EMA de 10 en un período de tiempo de 240 minutos. Ajuste el período de tiempo más alto o la duración de EMA para que coincida mejor con el entorno original.
- Asegúrese de que los `PriceStep` y `StepPrice` del instrumento estén configurados para que las paradas basadas en puntos se alineen con el tamaño del tick del corredor.
- El trailing stop solo se activa una vez que el precio avanza más de `TrailingStopPoints` desde el precio de entrada y nunca se afloja más allá del stop original.
