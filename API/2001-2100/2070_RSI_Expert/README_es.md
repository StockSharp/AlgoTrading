# RSI Experto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia **RSI Experto** opera usando el Índice de Fuerza Relativa (RSI). Espera a que el valor del RSI cruce niveles predefinidos de sobrecompra o sobreventa y entra en posiciones en la dirección del cruce.

## Lógica

- Calcular el RSI para cada vela.
- Cuando el RSI cruza **por encima** del nivel de sobreventa, se abre una posición larga.
- Cuando el RSI cruza **por debajo** del nivel de sobrecompra, se abre una posición corta.
- Antes de entrar en una nueva posición se cierra la opuesta.
- Se pueden activar protecciones opcionales de take‑profit, stop‑loss y trailing stop.

La estrategia procesa solo **velas terminadas** y usa la API de alto nivel de StockSharp con vinculación de indicadores.

## Parámetros

| Nombre | Descripción | Por defecto |
|--------|-------------|-------------|
| `RsiPeriod` | Período de cálculo del RSI. | `14` |
| `LevelUp` | Nivel de sobrecompra para activar cortos. | `70` |
| `LevelDown` | Nivel de sobreventa para activar largos. | `30` |
| `TakeProfitPercent` | Porcentaje de take profit. `0` desactiva. | `0` |
| `StopLossPercent` | Porcentaje de stop loss. `0` desactiva. | `0` |
| `TrailingStopPercent` | Porcentaje de trailing stop. `0` desactiva. | `0` |
| `CandleType` | Marco temporal de las velas para cálculos. | `1 minuto` |

## Notas

El trailing stop usa el mecanismo integrado `StartProtection`. Cuando `TrailingStopPercent` es mayor que cero reemplaza al stop loss regular y sigue automáticamente al precio.
