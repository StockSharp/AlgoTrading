# Estrategia de Regresión Cuadrática
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia calcula una línea de regresión cuadrática para las últimas `Length` barras y opera en los cruces del precio con la línea de regresión.

## Detalles

- **Criterios de entrada**: El precio cruza por encima/debajo de la línea de regresión cuadrática.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 54.
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame().
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Quadratic Regression
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
