# Configuración: Gaussian Suavizado + Adaptive Supertrend (Vol Manual) — Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Entra en largo cuando el cierre está por encima de una media móvil doblemente suavizada (tendencia "Gaussian").
Sale cuando el precio cierra por debajo de la línea de tendencia. Un filtro de volatilidad manual simple puede restringir las entradas.

## Detalles

- **Criterios de entrada**: Cierre por encima de la línea de tendencia y (filtro de volatilidad desactivado o volatilidad es 2 o 3).
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Cierre por debajo de la línea de tendencia.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `TrendLength` = 75
  - `Volatility` = 2
  - `EnableVolatilityFilter` = true
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Largo
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
