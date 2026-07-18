# Estrategia de Cruce Auto KD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia de Cruce Auto KD replica el ejemplo MQL5 `autoKD_EA`.  
Usa el indicador `StochasticOscillator` para generar señales de compra y venta basadas en cruces de las líneas %K y %D.

El cálculo base usa la fórmula RSV:
`RSV = (Close - LowestLow) / (HighestHigh - LowestLow) * 100`
donde el máximo más alto y el mínimo más bajo se calculan sobre las barras `KDPeriod`.  
La línea %K es una media móvil del RSV con longitud `KPeriod`; %D es una media móvil de %K con longitud `DPeriod`.

## Parámetros
| Nombre | Descripción | Predeterminado |
|--------|-------------|----------------|
| `KDPeriod` | Número de barras para el período base del RSV. | 30 |
| `KPeriod` | Período de suavizado para la línea %K. | 3 |
| `DPeriod` | Período de suavizado para la línea %D. | 6 |
| `CandleType` | Tipo y marco temporal de velas usadas para los cálculos. | Marco temporal de 5 minutos |
| `Volume` | Volumen de orden heredado de `Strategy`. | `Strategy.Volume` |

Todos los parámetros están disponibles para optimización.

## Lógica de Trading
1. Suscribirse a la serie de velas seleccionada y calcular el oscilador Estocástico.
2. Cuando el valor anterior de %K estaba por debajo de %D y el %K actual cruza por encima de %D, se abre una posición larga.
3. Cuando el valor anterior de %K estaba por encima de %D y el %K actual cruza por debajo de %D, se abre una posición corta.
4. La estrategia mantiene solo una posición a la vez. Los cruces en la dirección opuesta cierran la posición y abren el lado opuesto.
5. `StartProtection()` habilita los mecanismos de protección de pérdidas/ganancias predeterminados proporcionados por StockSharp.

## Visualización
La estrategia muestra automáticamente velas, el indicador Estocástico y los trades ejecutados en el gráfico.

## Notas
- Funciona con cualquier instrumento y marco temporal.
- Los parámetros deben adaptarse a la volatilidad del mercado seleccionado.
