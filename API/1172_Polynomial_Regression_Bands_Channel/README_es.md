# Estrategia de Canal de Bandas de Regresión Polinomial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia ajusta una línea de regresión polinomial a los precios recientes y construye bandas superior e inferior a partir de la desviación estándar de los residuos. Las posiciones largas se abren cuando el precio cae por debajo de la banda inferior y las posiciones cortas cuando el precio sube por encima de la banda superior.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close < LowerBand`.
  - **Corto**: `Close > UpperBand`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 100.
  - `Degree` = 2.
  - `Std Dev Multiplier` = 2.
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Regresión polinomial
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
