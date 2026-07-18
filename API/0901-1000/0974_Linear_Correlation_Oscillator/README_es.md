# Estrategia del Oscilador de Correlación Lineal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia del Oscilador de Correlación Lineal mide la correlación entre el precio y el tiempo en una ventana deslizante. La estrategia va largo cuando el oscilador cruza por encima de cero y va corto cuando cruza por debajo de cero.

## Detalles

- **Criterios de entrada**:
  - El oscilador cruza por encima de cero → **Largo**.
  - El oscilador cruza por debajo de cero → **Corto**.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Cruce de cero opuesto.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Length` = 14
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Linear Correlation
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
