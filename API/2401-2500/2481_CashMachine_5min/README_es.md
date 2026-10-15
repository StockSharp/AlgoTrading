# Estrategia CashMachine de 5 Minutos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión directa del Expert Advisor **CashMachine 5min** de MQL a la API de alto nivel de StockSharp. Está diseñada para velas de cinco minutos y combina el indicador DeMarker con un filtro de cruce del oscilador Estocástico. La gestión de operaciones utiliza niveles ocultos de stop-loss / take-profit junto con reglas de trailing por etapas que intentan asegurar ganancias una vez que aparece el momentum del precio.

## Lógica de trading
### Condiciones de entrada
- **Largo**: Valor anterior de DeMarker por debajo de 0.30 y valor actual en o por encima de 0.30 **y** el Estocástico %K cruza por encima de 20 en la misma vela. No debe haber ninguna posición abierta.
- **Corto**: Valor anterior de DeMarker por encima de 0.70 y valor actual en o por debajo de 0.70 **y** el Estocástico %K cruza por debajo de 80. No debe haber ninguna posición abierta.

### Gestión de posición
- Solo se mantiene una posición a la vez; las señales opuestas se ignoran hasta que la operación actual se cierra.
- Las salidas ocultas cierran la posición cuando el precio toca `Entry ± HiddenStopLoss` o `Entry ± HiddenTakeProfit` (valores interpretados en pips).
- Tres objetivos de ganancia intermedios (`TargetTp1/2/3`) mueven un trailing stop oculto a `precio actual - (objetivo - 13)` pips para largos y `precio actual + (objetivo + 13)` pips para cortos. Los 13 pips adicionales imitan el comportamiento del EA original, asegurando ganancias después de cada hito sin salir inmediatamente.
- Si el trailing stop es tocado después de la activación, la posición se cierra a mercado.

## Indicadores
- **DeMarker** – Detecta reversiones de momentum; el parámetro de longitud coincide con el período de promediado original.
- **Oscilador Estocástico** – Usa el período original de %K (`StochasticLength`), suavizado de %K (`StochasticK`) y suavizado de %D (`StochasticD`).

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `HiddenTakeProfit` | Distancia oculta del take-profit en pips. | 60 |
| `HiddenStopLoss` | Distancia oculta del stop-loss en pips. | 30 |
| `TargetTp1` | Primer nivel de activación del trailing (pips). | 20 |
| `TargetTp2` | Segundo nivel de activación del trailing (pips). | 35 |
| `TargetTp3` | Tercer nivel de activación del trailing (pips). | 50 |
| `DeMarkerLength` | Período de promediado del DeMarker. | 14 |
| `StochasticLength` | Período de lookback del Estocástico %K. | 5 |
| `StochasticK` | Longitud de suavizado de %K. | 3 |
| `StochasticD` | Longitud de suavizado de %D. | 3 |
| `CandleType` | Serie de velas usada para cálculos (por defecto 5 minutos). | Marco temporal de 5 minutos |

## Notas
- El tamaño del pip se deriva de `Security.PriceStep`. Cuando el paso es desconocido se usa un valor de respaldo de `0.0001`, reproduciendo la lógica del EA que ajusta para cotizaciones de 3 y 5 dígitos.
- Todas las decisiones de trading se basan en velas terminadas; el comportamiento intra-barra del EA original puede diferir ligeramente porque la versión MQL se ejecutaba en cada tick.
- La estrategia depende del manejo estándar del volumen de órdenes de StockSharp—establecer `Strategy.Volume` para controlar el tamaño de la operación.
