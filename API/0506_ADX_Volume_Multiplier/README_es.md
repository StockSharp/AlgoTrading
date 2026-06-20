# Estrategia de Multiplicador de Volumen ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Multiplicador de Volumen ADX combina la fortaleza de tendencia del Average Directional Index con un filtro de aumento de volumen. Entra en operaciones solo cuando el ADX supera un umbral, la línea direccional dominante apunta hacia la dirección de la tendencia y el volumen actual supera una media móvil multiplicada por un factor definido por el usuario.

## Detalles

- **Criterios de entrada**:
  - ADX por encima del umbral y DI+ > DI- con volumen mayor que SMA(volumen) * multiplicador → largo.
  - ADX por encima del umbral y DI- > DI+ con volumen mayor que SMA(volumen) * multiplicador → corto.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Una señal inversa activa la reversión de posición.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `AdxPeriod` = 21
  - `AdxThreshold` = 26
  - `VolumeMultiplier` = 1.8
  - `VolumePeriod` = 20
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ADX, Volume SMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
