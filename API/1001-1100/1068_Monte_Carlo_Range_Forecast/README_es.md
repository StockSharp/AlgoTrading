# Pronóstico de Rango Monte Carlo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Pronóstico de Rango Monte Carlo utiliza simulaciones Monte Carlo con volatilidad basada en ATR para proyectar el rango futuro de precios. La estrategia entra en largo cuando el precio simulado promedio supera el precio actual y entra en corto cuando cae por debajo.

## Detalles
- **Datos**: Velas de precio con ATR.
- **Criterios de entrada**:
  - **Largo**: El precio esperado de las simulaciones está por encima del precio actual.
  - **Corto**: El precio esperado de las simulaciones está por debajo del precio actual.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `ForecastPeriod` = 20
  - `Simulations` = 100
- **Filtros**:
  - Categoría: Estadística
  - Dirección: Largo y Corto
  - Indicadores: ATR
  - Complejidad: Moderado
  - Nivel de riesgo: Medio
