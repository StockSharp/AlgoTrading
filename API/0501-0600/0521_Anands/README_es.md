# Estrategia Anands
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este sistema de ruptura establece la dirección de la operación usando la vela del día anterior.
Si el cierre previo está por encima del máximo de ese día, la estrategia busca largos, mientras que un cierre por debajo del mínimo la vuelve bajista.
En el marco temporal de 15 minutos observa las últimas dos velas completadas.
Se abre una posición larga cuando la vela anterior cierra por encima del máximo de dos barras atrás.
Se abre una posición corta cuando el cierre anterior cae por debajo del mínimo de dos barras atrás.

## Detalles

- **Criterios de entrada**:
  - El cierre del día anterior por encima/por debajo de su rango establece el sesgo alcista/bajista.
  - **Largo**: cierre previo de 15m > máximo de dos barras atrás.
  - **Corto**: cierre previo de 15m < mínimo de dos barras atrás.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: No definidos, la señal inversa cierra.
- **Stops**: Sugerido en el lado opuesto de la barra de ruptura.
- **Valores predeterminados**:
  - `CandleType` = 15 minutos
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Velas
  - Stops: Opcional
  - Complejidad: Bajo
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
