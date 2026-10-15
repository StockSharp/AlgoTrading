# Estrategia de EA Vishal EURGBP H4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de EA Vishal EURGBP H4** replica el asesor experto original de MetaTrader que combina un filtro de entrada de cruzamiento estocástico con salidas basadas en envolventes. La lógica opera en velas H4 por defecto y usa herramientas virtuales de gestión de riesgo (stop-loss, take-profit y trailing stop opcional) definidas en pips, imitando de cerca el comportamiento de MT4.

## Lógica de trading
- **Entrada** – la estrategia espera un cruzamiento estocástico evaluado en las dos velas completadas más recientes. Una posición larga se abre cuando %K cruza por debajo de %D entre la barra *n-2* y *n-1*. Una posición corta se abre en el cruzamiento opuesto. Solo puede estar activa una posición a la vez.
- **Salida** – las posiciones activas se gestionan en tres capas:
  1. **Ruptura de envolvente** – si la siguiente barra abre más allá de la banda de envolvente anterior mientras la barra anterior abrió dentro, la posición se cierra inmediatamente.
  2. **Stop-loss / take-profit virtual** – los precios objetivo se calculan desde el precio de entrada usando las distancias de pip configuradas.
  3. **Trailing stop opcional** – cuando está habilitado y un stop-loss está definido, el nivel de stop sigue el valor más alto (para largos) o más bajo (para cortos) de la vela anterior menos/más la distancia de stop.

## Parámetros
| Nombre | Predeterminado | Descripción |
| ------ | -------------- | ----------- |
| `Volume` | 0.5 | Volumen de la orden en lotes para cada operación. |
| `StopLossPips` | 0 | Distancia hard stop-loss en pips (0 deshabilita el stop). |
| `TakeProfitPips` | 22 | Distancia de take-profit en pips (0 deshabilita el objetivo). |
| `UseTrailingStop` | false | Habilita el trailing stop virtual que sigue el extremo de la vela anterior. Requiere `StopLossPips` &gt; 0. |
| `StochasticKPeriod` | 6 | Período de lookback para el cálculo del %K estocástico. |
| `StochasticDPeriod` | 3 | Período de suavizado para la línea %D. |
| `StochasticSlowing` | 1 | Factor de ralentización aplicado a %K. |
| `EnvelopePeriod` | 32 | Longitud del SMA usado como base del envolvente. |
| `EnvelopeDeviationPercent` | 0.3 | Desviación en porcentaje aplicada por encima/debajo del SMA para construir los envolventes. |
| `CandleType` | Marco temporal H4 | Serie de velas que alimenta la estrategia (por defecto son velas de cuatro horas). |

## Notas
- Todos los parámetros están expuestos para optimización en StockSharp Studio.
- Los niveles protectores se rastrean internamente y se ejecutan con órdenes de mercado cuando el rango de la vela los perfora, coincidiendo con el comportamiento del asesor experto original en eventos de nueva barra.
- La estrategia se basa únicamente en velas terminadas, asegurando backtests deterministas y comportamiento en producción.
