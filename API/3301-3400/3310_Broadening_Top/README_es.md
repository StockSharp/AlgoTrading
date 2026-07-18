# Estrategia Broadening Top
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Broadening Top Strategy es un sistema seguidor de tendencia inspirado en el asesor experto original de MetaTrader "Broadening top". La estrategia se centra en capturar rupturas que aparecen después de una formación expansiva combinando dirección de tendencia y confirmación de momentum. Dos medias móviles ponderadas lineales, un oscilador de momentum y un filtro MACD trabajan juntos para detectar rupturas alcistas y bajistas.

## Lógica de negociación
1. **Filtro de tendencia:** la estrategia compara una media móvil ponderada lineal (LWMA) rápida y una lenta. Las operaciones largas requieren que la LWMA rápida esté por encima de la lenta, mientras que las cortas esperan lo contrario.
2. **Confirmación de momentum:** el oscilador de momentum se observa en las tres últimas velas completadas. Una operación solo se permite si cualquiera de estos valores se desvía del nivel neutral (100) al menos por el umbral configurado (valores separados para largos y cortos).
3. **Alineación MACD:** un filtro adicional comprueba la línea MACD frente a su línea de señal. Las posiciones largas solo se activan cuando la línea MACD está por encima de la señal; las cortas, cuando está por debajo.
4. **Manejo de posición:** antes de abrir una operación en la dirección opuesta, la estrategia cierra la posición actual, garantizando que solo haya una posición activa a la vez.

## Gestión de riesgo
La estrategia usa `StartProtection` para gestionar órdenes protectoras:
- Distancias opcionales de stop-loss y take-profit definidas en pasos de precio (pips).
- Un trailing stop opcional con paso de trailing configurable.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `OrderVolume` | Tamaño de orden en lotes/contratos. | 1 |
| `FastMaLength` | Longitud de la media móvil ponderada lineal rápida. | 6 |
| `SlowMaLength` | Longitud de la media móvil ponderada lineal lenta. | 85 |
| `MomentumPeriod` | Periodo de retrospección del oscilador de momentum. | 14 |
| `MomentumBuyThreshold` | Distancia mínima desde el nivel neutral de momentum (100) requerida para permitir entradas largas. | 0.3 |
| `MomentumSellThreshold` | Distancia mínima desde el nivel neutral de momentum (100) requerida para permitir entradas cortas. | 0.3 |
| `MacdFast` | Longitud de EMA rápida dentro del MACD. | 12 |
| `MacdSlow` | Longitud de EMA lenta dentro del MACD. | 26 |
| `MacdSignal` | EMA de señal dentro del MACD. | 9 |
| `TakeProfitPips` | Distancia de take-profit medida en pasos de precio. | 50 |
| `StopLossPips` | Distancia de stop-loss medida en pasos de precio. | 20 |
| `TrailingStopPips` | Distancia de trailing-stop medida en pasos de precio. | 40 |
| `TrailingStepPips` | Distancia adicional antes de actualizar el trailing stop. | 10 |
| `CandleType` | Tipo de vela/marco temporal usado para cálculos. | Marco de 15 minutos |
| `EnableLongs` | Activa o desactiva operaciones largas. | true |
| `EnableShorts` | Activa o desactiva operaciones cortas. | true |

## Indicadores
- **LinearWeightedMovingAverage:** filtros de tendencia rápido y lento.
- **Momentum:** confirma la aceleración del mercado alejándose del nivel neutral.
- **MovingAverageConvergenceDivergenceSignal:** proporciona confirmación direccional mediante MACD y líneas de señal.

## Notas de uso
- Los umbrales de momentum se evalúan en las tres velas completadas más recientes para emular el comportamiento MQL original.
- Las órdenes protectoras (stop-loss, take-profit, trailing stop) son opcionales y pueden desactivarse poniendo la distancia correspondiente en cero.
- La estrategia debe adjuntarse a instrumentos que proporcionen paso de precio e información decimal para calcular correctamente el tamaño de pip.
