# Kolier SuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Kolier SuperTrend que aplica bandas ATR para detectar reversiones de tendencia.

El indicador dibuja niveles dinámicos de soporte y resistencia derivados del ATR. Una reversión alcista ocurre cuando el precio cierra por encima de la banda inferior y la línea gira por debajo del precio. Una reversión bajista ocurre cuando el precio cierra por debajo de la banda superior.

Siguiendo este rastro adaptativo, la estrategia intenta capturar tendencias fuertes mientras se mantiene protegida cuando el impulso disminuye.

## Detalles

- **Criterios de entrada**: El precio cruza la línea SuperTrend.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `Period` = 10
  - `Multiplier` = 3.0m
  - `CandleType` = TimeSpan.FromHours(4)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, SuperTrend
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Swing (4h)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
