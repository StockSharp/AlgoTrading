# Estrategia de Reversión a la Media IBS de Fuerza Interna de la Barra
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de reversión a la media solo en corto que utiliza la Fuerza Interna de la Barra (IBS). Toma posición corta cuando el IBS es alto y el precio supera el máximo anterior; cierra cuando el IBS cae por debajo del umbral inferior.

## Detalles

- **Criterios de entrada**: IBS >= umbral superior y cierre > máximo anterior
- **Largo/Corto**: Corto
- **Criterios de salida**: IBS <= umbral inferior
- **Stops**: No
- **Valores predeterminados**:
  - `UpperThreshold` = 0.9
  - `LowerThreshold` = 0.3
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Corto
  - Indicadores: IBS
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
