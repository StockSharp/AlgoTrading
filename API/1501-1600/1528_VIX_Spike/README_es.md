# Estrategia de Pico VIX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Compra cuando el índice VIX sube por encima de su media móvil en un múltiplo de la desviación estándar y cierra después de un número fijo de barras.

## Detalles

- **Criterios de entrada**: VIX > media + StdDevMultiplier * desviación estándar.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Salir después de `ExitPeriods` barras.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StdDevLength` = 15
  - `StdDevMultiplier` = 2
  - `ExitPeriods` = 10
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `VixSecurity` = "CBOE:VIX"
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Solo largos
  - Indicadores: SMA, StdDev
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
