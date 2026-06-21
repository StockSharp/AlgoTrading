# Estrategia de Cruce de Medias Móviles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Compra cuando la SMA corta cruza por encima de la SMA larga y vende cuando cruza por debajo. Las posiciones se revierten ante señales opuestas.

## Detalles

- **Criterios de entrada**:
  - Largo cuando la SMA corta cruza por encima de la SMA larga.
  - Corto cuando la SMA corta cruza por debajo de la SMA larga.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Reversión ante cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `ShortLength` = 9
  - `LongLength` = 21
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Crossover
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
