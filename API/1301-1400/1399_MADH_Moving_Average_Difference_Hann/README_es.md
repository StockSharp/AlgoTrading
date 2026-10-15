# Estrategia MADH de Diferencia de Medias Móviles, Hann
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementa el indicador MADH descrito por John Ehlers. La estrategia va largo cuando el indicador está por encima de cero y corto cuando está por debajo.

## Detalles
- **Criterios de entrada**: MADH > 0 para largos, MADH < 0 para cortos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Revertir con señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `ShortLength` = 8
  - `DominantCycle` = 27
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: MADH
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
