# Estrategia de Ruptura de Triángulo con TP, SL y Filtro EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Detecta patrones de triángulo a partir de máximos y mínimos de pivote. Entra en largo en la ruptura por encima del triángulo, opcionalmente requiriendo que el precio esté por encima de EMA20 y EMA50, y utiliza take-profit y stop-loss basados en porcentaje.

## Detalles

- **Criterios de entrada**: cierre por encima de la línea superior del triángulo con filtro opcional EMA20/EMA50
- **Largo/Corto**: Largo
- **Criterios de salida**: take-profit o stop-loss en porcentaje
- **Stops**: Sí
- **Valores predeterminados**:
  - `PivotLength` = 5
  - `TakeProfitPercent` = 3
  - `StopLossPercent` = 1.5
  - `UseEmaFilter` = true
  - `EmaFast` = 20
  - `EmaSlow` = 50
  - `CandleType` = 1 hora
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Largo
  - Indicadores: EMA, Pivot
  - Stops: Sí
  - Complejidad: Avanzado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
