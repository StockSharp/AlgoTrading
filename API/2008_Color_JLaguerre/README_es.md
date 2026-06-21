# Color JLaguerre
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el oscilador Laguerre codificado por colores.

El indicador suaviza el movimiento de precios con un filtro Jurik y colorea su línea de acuerdo a la posición dentro de niveles predefinidos. Un cambio de color marca un posible cambio de tendencia.

La estrategia entra largo cuando el oscilador cruza el nivel medio hacia arriba y corto cuando lo cruza hacia abajo. Las posiciones se cierran cuando el oscilador alcanza niveles extremos o aparece una señal opuesta.

## Detalles

- **Criterios de entrada**: Cambio de color del oscilador Laguerre alrededor del nivel medio.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o alcance de nivel extremo.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `HighLevel` = 85
  - `MiddleLevel` = 50
  - `LowLevel` = 15
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromHours(1)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Por hora (1h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
