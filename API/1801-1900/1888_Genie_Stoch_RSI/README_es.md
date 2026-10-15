# Estrategia Genie Stoch RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera utilizando una combinación del Índice de Fuerza Relativa (RSI) y el Oscilador Estocástico.
Espera a que el mercado alcance zonas de sobrecompra o sobreventa y luego busca un cruce entre la línea principal
del Estocástico y su línea de señal para confirmar la reversión. Se aplican un trailing stop y un take profit fijo
para la gestión del riesgo.

## Lógica

1. Suscribirse a velas del marco temporal seleccionado.
2. Calcular el RSI con un período configurable.
3. Calcular el Oscilador Estocástico con períodos %K, %D y de desaceleración configurables.
4. Para una entrada larga:
   - El RSI está por debajo del nivel de sobreventa.
   - %K está por debajo del nivel de sobreventa del Estocástico.
   - El %K anterior está por debajo del %D anterior y el %K actual cruza al alza el %D actual.
5. Para una entrada corta:
   - El RSI está por encima del nivel de sobrecompra.
   - %K está por encima del nivel de sobrecompra del Estocástico.
   - El %K anterior está por encima del %D anterior y el %K actual cruza a la baja el %D actual.
6. El tamaño de la posición se toma de la propiedad `Volume` de la estrategia. Las posiciones existentes se revierten cuando
   aparece una señal contraria.
7. `StartProtection` habilita un trailing stop y take profit medidos en puntos de precio.

## Parámetros

| Nombre | Descripción |
| ---- | ----------- |
| `RsiPeriod` | Longitud del cálculo del RSI. |
| `KPeriod` | Período %K del Estocástico. |
| `DPeriod` | Período %D del Estocástico. |
| `Slowing` | Valor de desaceleración del Estocástico. |
| `RsiOverbought` | Nivel del RSI considerado sobrecomprado. |
| `RsiOversold` | Nivel del RSI considerado sobrevendido. |
| `StochOverbought` | Nivel del Estocástico considerado sobrecomprado. |
| `StochOversold` | Nivel del Estocástico considerado sobrevendido. |
| `TakeProfit` | Distancia del take profit en puntos de precio. |
| `TrailingStop` | Distancia del trailing stop en puntos de precio. |
| `CandleType` | Tipo y marco temporal de velas para analizar. |

## Notas

La estrategia procesa solo velas finalizadas e ignora cualquier señal hasta que todos los indicadores estén completamente formados.
Está diseñada como un ejemplo educativo y debe probarse exhaustivamente antes de operar en tiempo real.
