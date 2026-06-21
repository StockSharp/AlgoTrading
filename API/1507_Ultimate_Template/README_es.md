# Plantilla de Estrategia Ultimate
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Plantilla básica de cruce de medias móviles que abre posiciones largas o cortas cuando las medias rápida y lenta se cruzan. Incluye protecciones opcionales de stop loss y take profit en porcentaje.

## Detalles

- **Criterios de entrada**: Cruce de la SMA rápida sobre la SMA lenta.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Cruce opuesto o protecciones de riesgo.
- **Stops**: Stop loss y take profit en porcentaje.
- **Valores predeterminados**:
  - `FastLength` = 9
  - `SlowLength` = 21
  - `StopLossPercent` = 1
  - `TakeProfitPercent` = 3
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Medio
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
