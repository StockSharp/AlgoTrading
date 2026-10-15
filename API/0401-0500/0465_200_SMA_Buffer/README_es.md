# Estrategia de Buffer de 200 SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Buffer de 200 SMA opera basándose en la distancia del precio respecto a una media móvil simple de largo plazo. Compra cuando el cierre sube un cierto porcentaje por encima de la SMA y sale cuando el precio cae un porcentaje definido por debajo de ella. El enfoque busca capturar el momentum a largo plazo permitiendo un búfer alrededor de la media móvil.

## Detalles

- **Criterios de entrada**:
  - Precio de cierre > SMA * (1 + Entry %).
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Precio de cierre < SMA * (1 - Exit %).
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `SmaLength` = 200
  - `EntryPercent` = 5
  - `ExitPercent` = 3
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Long
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
