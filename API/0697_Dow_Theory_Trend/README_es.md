# Estrategia de Tendencia de la Teoría de Dow
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Tendencia de la Teoría de Dow utiliza máximos y mínimos pivote para determinar la dirección de la tendencia. La estrategia entra en largo cuando aparecen tanto máximos más altos como mínimos más altos, y entra en corto cuando se forman tanto máximos más bajos como mínimos más bajos.

## Detalles

- **Criterios de entrada**: Máximos y mínimos más altos para largo; máximos y mínimos más bajos para corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal inversa.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `PivotLookback` = 10
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Acción del precio
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
