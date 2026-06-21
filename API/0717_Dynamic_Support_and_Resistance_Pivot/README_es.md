# Estrategia Dinámica de Pivote de Soporte y Resistencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia deriva niveles dinámicos de soporte y resistencia a partir de máximos y mínimos de pivote recientes. Entra largo cuando el precio cruza por encima del soporte cerca del nivel y entra corto cuando el precio cruza por debajo de la resistencia. La gestión de riesgos utiliza niveles fijos de stop-loss y toma de ganancias en porcentaje.

## Detalles

- **Criterios de entrada**: Precio cerca del soporte/resistencia dentro del porcentaje `SupportResistanceDistance` y cruce por encima del soporte o por debajo de la resistencia.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Toma de ganancias y stop-loss fijos.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `PivotLength` = 2
  - `SupportResistanceDistance` = 0.4m
  - `StopLossPercent` = 10.0m
  - `TakeProfitPercent` = 26.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Pivot
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
