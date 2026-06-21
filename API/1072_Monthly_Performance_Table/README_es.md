# Estrategia de Tabla de Rendimiento Mensual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera cuando el ADX se encuentra entre +DI y -DI y ambas diferencias respecto al ADX superan umbrales configurables.

## Detalles

- **Criterios de entrada**:
  - Largo cuando |+DI - ADX| ≥ `LongDifference` y |-DI - ADX| ≥ `LongDifference` con ADX entre +DI y -DI.
  - Corto cuando |+DI - ADX| ≥ `ShortDifference` y |-DI - ADX| ≥ `ShortDifference` con ADX entre -DI y +DI.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal inversa.
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 14
  - `LongDifference` = 10
  - `ShortDifference` = 10
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ADX, DMI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
