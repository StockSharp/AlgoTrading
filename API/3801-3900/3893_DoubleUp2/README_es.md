# Estrategia DoubleUp2 Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia DoubleUp2 Martingale reproduce el experto original MetaTrader combinando el índice del canal de productos básicos (CCI) y el oscilador MACD. Las operaciones se abren sólo cuando ambos indicadores alcanzan niveles extremos en la misma dirección. El tamaño de la posición sigue un esquema de martingala en el que el volumen se duplica después de una operación perdedora. Las operaciones rentables se bloquean parcialmente cerrando la posición una vez que el precio recorre una distancia configurable a favor de la posición.

## Cómo funciona
1. Suscríbase a una única serie de velas (por defecto, 1 minuto) y calcule CCI y MACD en cada barra completa.
2. Detectar impulso extremo:
   * Ingrese short cuando tanto CCI como MACD excedan el umbral positivo.
   * Ingrese largo cuando ambos caigan por debajo del umbral negativo.
3. Antes de revertir, la posición actual se cierra y el paso de martingala se actualiza en función del beneficio simulado de la última operación.
4. El volumen comercial es igual al volumen base derivado del capital de la cuenta dividido por un divisor de saldo, multiplicado por el factor martingala elevado al paso actual.
5. Consiga ganancias cerrando cualquier posición abierta una vez que el precio avance una cantidad predefinida de puntos desde la última entrada. Las salidas ganadoras aumentan el paso de martingala en dos para igualar el comportamiento original de EA.

## Parámetros
| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `CciPeriod` | Período retroactivo para el indicador CCI. | 8 |
| `MacdFastPeriod` | Longitud rápida de EMA para MACD. | 13 |
| `MacdSlowPeriod` | Longitud lenta de EMA para MACD. | 33 |
| `MacdSignalPeriod` | Longitud de la señal EMA para el suavizado MACD. | 2 |
| `Threshold` | Umbral absoluto del indicador que debe superarse para activar las entradas. | 230 |
| `ExitDistancePoints` | Distancia de ganancia en puntos que desencadena el cierre de posición. | 120 |
| `BalanceDivisor` | Divisor aplicado al capital de la cartera para obtener el volumen base. | 50001 |
| `MinimumVolume` | Límite inferior para el volumen comercial calculado. | 0.1 |
| `MartingaleMultiplier` | Multiplicador aplicado al tamaño de la posición después de cada cierre perdedor. | 2 |
| `CandleType` | Periodo de velas utilizado para todos los cálculos. | 1 minuto |

## Notas
* La lógica de martingala aumenta el tamaño de la posición después de pérdidas y se reinicia después de reversiones rentables, reflejando la lógica fuente MQL.
* La información del paso del precio se utiliza para convertir la distancia de salida (puntos) en unidades de precio absoluto. Si el instrumento no proporciona un escalón de precio, se utiliza un valor de 1.
* La estrategia espera un único instrumento y no coloca posiciones largas y cortas simultáneas.
