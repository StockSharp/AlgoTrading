# Estrategia BykovTrend ReOpen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia BykovTrend ReOpen utiliza la lógica de BykovTrend basada en los indicadores Williams %R y Average True Range. Una señal de compra ocurre cuando la tendencia se vuelve alcista y una señal de venta cuando se vuelve bajista. Después de entrar en una posición, la estrategia puede reabrir posiciones adicionales en cada paso de precio predefinido mientras la tendencia continúa. El stop loss y el take profit se aplican desde el precio del último entrada.

## Indicador
La estrategia no requiere un archivo de indicador separado. Calcula las señales utilizando:
- **Williams %R** con período `SSP`.
- **ATR** con período fijo de 15.
La tendencia cambia cuando Williams %R cruza los umbrales `-100 + K` y `-K`, donde `K = 33 - Risk`.

## Reglas de operación
1. En una señal alcista, cierra las posiciones cortas (si está permitido) y abre una posición larga.
2. En una señal bajista, cierra las posiciones largas (si está permitido) y abre una posición corta.
3. Mientras una posición está abierta, se añaden nuevas posiciones en la misma dirección cada `Price Step` unidades hasta alcanzar `Max Positions`.
4. Cada posición tiene distancias de stop loss y take profit medidas desde el último precio de entrada.

## Parámetros
- `Risk` – factor de riesgo que define los umbrales del indicador.
- `SSP` – período de Williams %R.
- `Price Step` – distancia de precio para añadir una nueva posición.
- `Max Positions` – número máximo de posiciones abiertas por lado.
- `Stop Loss` – distancia de stop loss en unidades de precio.
- `Take Profit` – distancia de take profit en unidades de precio.
- `Enable Long Open` – permitir la apertura de posiciones largas.
- `Enable Short Open` – permitir la apertura de posiciones cortas.
- `Enable Long Close` – permitir el cierre de posiciones largas en señal contraria.
- `Enable Short Close` – permitir el cierre de posiciones cortas en señal contraria.
- `Candle Type` – marco temporal utilizado para los cálculos.
