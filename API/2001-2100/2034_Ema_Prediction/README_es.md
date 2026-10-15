# Estrategia de Predicción EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador EMA Prediction que genera señales cuando las medias móviles exponenciales rápida y lenta se cruzan en una vela que confirma la dirección.

La estrategia abre posiciones largas cuando la EMA rápida cruza por encima de la EMA lenta durante una vela alcista y cierra cualquier posición corta. Abre posiciones cortas cuando la EMA rápida cruza por debajo de la EMA lenta durante una vela bajista y cierra cualquier posición larga.

## Detalles

- **Criterios de entrada**:
  - Largo: la EMA rápida cruza por encima de la EMA lenta y la vela es alcista.
  - Corto: la EMA rápida cruza por debajo de la EMA lenta y la vela es bajista.
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: Take profit y stop loss fijos
- **Valores predeterminados**:
  - `CandleType` = velas de 6 horas
  - `FastPeriod` = 1
  - `SlowPeriod` = 2
  - `StopLossTicks` = 1000
  - `TakeProfitTicks` = 2000
- **Filtros**:
  - Categoría: Cruce de medias móviles
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: Take profit y stop loss
  - Complejidad: Básico
  - Marco temporal: 6 horas
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
