# MA con Función Logística
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

A pesar de su nombre histórico, esta implementación es una estrategia de cruce de dos EMA y no calcula un modelo logístico. Los cruces en velas finalizadas abren o invierten la posición, y el take-profit y stop-loss porcentuales se miden desde los precios de ejecución.

## Detalles
- **Datos**: Velas de precios.
- **Criterios de entrada**:
  - **Largo**: la EMA rápida cruza por encima de la EMA lenta.
  - **Corto**: la EMA rápida cruza por debajo de la EMA lenta.
- **Criterios de salida**: se alcanza `TakeProfitPercent` o `StopLossPercent`; un cruce opuesto invierte la posición.
- **Pausa**: cinco velas finalizadas después de cada orden por cruce.
- **Valores predeterminados**:
  - `FastLength` = 12
  - `SlowLength` = 25
  - `TakeProfitPercent` = 8
  - `StopLossPercent` = 5
  - `CandleType` = 20 minutos
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: MA
  - Complejidad: Bajo
  - Nivel de riesgo: Medio
