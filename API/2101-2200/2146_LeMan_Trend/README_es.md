# Estrategia LeMan Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia LeMan Tendencia deriva presión alcista y bajista a partir de máximos y mínimos recientes. Calcula la distancia entre la vela actual y los máximos más altos y los mínimos más bajos durante tres períodos de retroceso diferentes. Estas distancias se suavizan con una media móvil exponencial (EMA) para formar dos líneas: bulls (alcistas) y bears (bajistas). Un cruce entre estas líneas señala posibles cambios de tendencia.

Cuando la línea bulls cruza por encima de la línea bears, la estrategia abre una posición larga o cierra una posición corta existente. Por el contrario, cuando la línea bears se mueve por encima de la línea bulls, abre una posición corta o sale de una larga. El método no utiliza filtros adicionales, centrándose únicamente en la fortaleza relativa de los máximos y mínimos recientes.

## Detalles

- **Criterios de entrada**
  - **Largo**: La línea bulls cruza por encima de la línea bears.
  - **Corto**: La línea bears cruza por encima de la línea bulls.
- **Largo/Corto**: Ambos lados compatibles.
- **Criterios de salida**
  - El cruce opuesto cierra la posición activa.
- **Stops**: Ninguno por defecto.
- **Valores predeterminados**
  - `Min` = 13
  - `Midle` = 21
  - `Max` = 34
  - `EMA period` = 3
  - `Time frame` = 4 hours
- **Filtros**
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Highest, Lowest, EMA
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
