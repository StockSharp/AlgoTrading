# Estrategia Bedo Osaimi Istr
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Una sencilla estrategia de seguimiento de tendencia que compara medias móviles de los precios de cierre y apertura. Se abre una posición larga cuando la media móvil del cierre cruza por encima de la media móvil de la apertura. La posición se invierte cuando se produce el cruce opuesto.

## Detalles

- **Criterios de entrada**:
  - La MA del cierre cruza por encima de la MA de la apertura.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**:
  - La MA del cierre cruza por debajo de la MA de la apertura (para salida larga o entrada corta).
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `MaLength` = 20
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA en cierre y apertura
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
