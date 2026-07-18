# Estrategia The Puncher
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convertido del asesor experto de MetaTrader 5 "The Puncher".
- Combina un oscilador Estocástico de largo período con RSI para identificar zonas de agotamiento.
- Opera únicamente cuando la vela actual está cerrada, siguiendo el enfoque de la API de alto nivel de StockSharp.
- Aplica lógica de stop-loss protector, take-profit, punto de equilibrio y stop móvil para gestionar el riesgo.

## Indicadores
- **Oscilador Estocástico**: período base `StochasticPeriod`, suavizado %K `StochasticSignalPeriod`, suavizado %D `StochasticSmoothingPeriod`.
- **Índice de Fuerza Relativa (RSI)**: período `RsiPeriod`.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `StochasticPeriod` | 100 | Período base del oscilador Estocástico. |
| `StochasticSignalPeriod` | 3 | Período de suavizado aplicado a la línea %K. |
| `StochasticSmoothingPeriod` | 3 | Período de suavizado aplicado a la línea %D. |
| `RsiPeriod` | 14 | Longitud de cálculo del RSI. |
| `OversoldLevel` | 30 | Umbral compartido por el Estocástico y el RSI para detectar zonas de sobreventa. |
| `OverboughtLevel` | 70 | Umbral compartido por el Estocástico y el RSI para detectar zonas de sobrecompra. |
| `StopLossPips` | 20 | Distancia del stop-loss en pips (0 deshabilita el stop-loss). |
| `TakeProfitPips` | 50 | Distancia del take-profit en pips (0 deshabilita el take-profit). |
| `TrailingStopPips` | 10 | Distancia del stop móvil en pips (0 deshabilita el trailing). |
| `TrailingStepPips` | 5 | Movimiento favorable mínimo en pips requerido antes de volver a ajustar el stop móvil. |
| `BreakEvenPips` | 21 | Ganancia en pips requerida antes de mover el stop al punto de equilibrio (0 deshabilita). |
| `CandleType` | Marco temporal de 5 minutos | Tipo de vela utilizado para los cálculos. |
| `Volume` | Propiedad de la estrategia | Tamaño de la orden utilizado para las entradas (configurado mediante `Volume` de la estrategia). |

> **Manejo de pips**: los parámetros basados en pips se convierten a precios absolutos usando `Security.PriceStep`. Ajuste `Security.PriceStep` según el instrumento que opere.

## Reglas de trading
### Entrada
- **Largo**: cuando la línea de señal del Estocástico y el RSI caen por debajo de `OversoldLevel`, y no existe una posición larga.
- **Corto**: cuando la línea de señal del Estocástico y el RSI suben por encima de `OverboughtLevel`, y no existe una posición corta.
- Si aparece una señal contraria mientras hay una posición abierta, la estrategia cierra la posición y espera a la siguiente vela antes de considerar nuevas entradas.

### Salida y gestión del riesgo
- **Stop-loss**: distancia fija definida por `StopLossPips`.
- **Take-profit**: objetivo fijo definido por `TakeProfitPips`.
- **Punto de equilibrio**: una vez que la ganancia alcanza `BreakEvenPips`, el stop se mueve al precio de entrada.
- **Stop móvil**: después de que el precio se mueve favorablemente en `TrailingStopPips`, el stop sigue al mercado y se ajusta cada `TrailingStepPips`.
- **Señales contrarias**: fuerzan una salida incluso si no se ha alcanzado el stop o el objetivo.

## Notas
- Funciona con cualquier instrumento compatible con StockSharp; los valores predeterminados están ajustados para pips de divisas.
- Usa únicamente velas completadas, reproduciendo el comportamiento `TradeAtCloseBar=true` del robot original.
- Configure el portafolio, el instrumento y el volumen antes de iniciar la estrategia.
