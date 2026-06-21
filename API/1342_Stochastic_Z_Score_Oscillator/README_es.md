# Estrategia de Oscilador Stochastic Z-Score
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Combina un oscilador Stochastic reescalado con un Z-Score de precio. Se abre una operación cuando su promedio cruza un umbral y se cierra cuando el Z-Score regresa a cero. Los contadores de enfriamiento previenen señales frecuentes.

## Detalles

- **Criterios de entrada**: el promedio del %K del Stochastic reescalado y el Z-Score del precio cruza por encima/debajo del umbral tras el enfriamiento
- **Largo/Corto**: Ambos
- **Criterios de salida**: Z-Score cruzando cero
- **Stops**: No
- **Valores predeterminados**:
  - `RollingWindow` = 80
  - `ZThreshold` = 2.8
  - `CoolDown` = 5
  - `StochLength` = 14
  - `StochSmooth` = 7
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Stochastic, SMA, StandardDeviation
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
