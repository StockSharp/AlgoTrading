# Estrategia Adam and Eve
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que combina velas Heiken Ashi con una cascada de medias móviles simples. Se abre una posición corta cuando aparece una vela Heiken Ashi bajista sin sombra superior y todas las medias móviles monitorizadas (5, 7, 9, 10, 12, 14, 20) apuntan hacia abajo. Una posición larga se desencadena por una vela alcista sin sombra inferior y todas las medias apuntando hacia arriba. Cada operación tiene como objetivo un beneficio a una distancia de un ATR(14) desde la entrada sin stop-loss.

## Detalles

- **Criterios de entrada**: vela Heiken Ashi previa sin sombra superior (corto) o inferior (largo) y pila de SMA alineada
- **Largo/Corto**: Ambos
- **Criterios de salida**: objetivo de beneficio a distancia ATR(14)
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `AtrPeriod` = 14
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA (5,7,9,10,12,14,20), Heiken Ashi, ATR
  - Stops: Solo objetivo
  - Complejidad: Intermedio
  - Marco temporal: Configurable, por defecto 15 minutos
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
