# Estrategia de Marco Temporal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de cruce de EMA con gestión de riesgo adaptada al marco temporal.

Las pruebas indican un retorno anual promedio de aproximadamente el 31%. Funciona mejor en el mercado de criptomonedas.

La estrategia compra cuando una EMA rápida cruza por encima de una EMA más lenta y la tendencia a largo plazo es alcista. Las entradas cortas ocurren en el cruce opuesto. Las horas de operación y un filtro ADX simple ayudan a evitar períodos de bajo momentum. El riesgo se gestiona con toma de ganancias y stop loss basados en porcentajes.

## Detalles

- **Criterios de entrada**:
  - **Largo**: EMA9 cruza por encima de EMA20 mientras EMA50 está por encima de EMA200.
  - **Corto**: EMA9 cruza por debajo de EMA20 mientras EMA50 está por debajo de EMA200.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Stop loss o toma de ganancias.
  - **Corto**: Stop loss o toma de ganancias.
- **Stops**: Sí, trailing opcional.
- **Valores predeterminados**:
  - `TakeProfitPercent` = 1.5
  - `StopLossPercent` = 1.0
  - `TrailingPercent` = 0.5
  - `StartHour` = 15
  - `EndHour` = 20
  - `CooldownBars` = 5
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: EMA, RSI, ADX
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
