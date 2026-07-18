# Estrategia Bitcoin Sentimiento de Apalancamiento
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia analiza el Z-Score de la relación entre posiciones largas y cortas en Bitcoin. Se abre una operación larga cuando el Z-Score cruza por encima de un umbral configurable y se cierra cuando cruza por debajo del nivel de salida largo. Las operaciones cortas utilizan umbrales espejados. La dirección de trading puede limitarse a largo, corto o ambos lados.

## Detalles

- **Criterios de entrada**:
  - Z-Score cruza por encima del umbral de entrada largo → largo.
  - Z-Score cruza por debajo del umbral de entrada corto → corto.
- **Largo/Corto**: Configurable
- **Criterios de salida**:
  - Z-Score cruza por debajo del umbral de salida largo.
  - Z-Score cruza por encima del umbral de salida corto.
- **Stops**: Ninguno
- **Valores predeterminados**:
  - Longitud Z-Score = 252
  - Entrada largo = 1.0
  - Salida largo = -1.618
  - Entrada corto = -1.618
  - Salida corto = 1.0
  - Tipo de vela = 1 día
- **Filtros**:
  - Categoría: Sentiment
  - Dirección: Ambos
  - Indicadores: SMA, StdDev
  - Stops: Ninguno
  - Complejidad: Bajo
  - Marco temporal: Largo plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
