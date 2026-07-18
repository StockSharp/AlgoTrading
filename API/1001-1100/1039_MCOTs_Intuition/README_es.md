# Estrategia MCOTs Intuition
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el momentum del RSI relativo a su desviación estándar. Compra cuando el momentum alcista es fuerte pero se desvanece y vende en condiciones opuestas. Se colocan objetivos de ganancia fijos y stop loss en ticks.

## Detalles

- **Criterios de entrada**:
  - Largo: momentum > stdDev * multiplier y momentum < previousMomentum * exhaustionMultiplier
  - Corto: momentum < -stdDev * multiplier y momentum > previousMomentum * exhaustionMultiplier
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Objetivo de ganancia fijo y stop loss en ticks
- **Stops**: Sí
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `StdDevMultiplier` = 1m
  - `ExhaustionMultiplier` = 1m
  - `ProfitTargetTicks` = 40
  - `StopLossTicks` = 160
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: RSI, StandardDeviation
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
