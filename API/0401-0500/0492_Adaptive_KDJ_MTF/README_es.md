# KDJ Adaptativo (MTF)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia KDJ Adaptativo combina valores del oscilador KDJ de tres marcos temporales. Cada marco temporal se suaviza con una EMA y se combina utilizando pesos ajustables. La fuerza de la tendencia se mide con una SMA del oscilador combinado, que adapta los niveles de sobrecompra y sobreventa.

La estrategia entra en largo cuando la línea J está por debajo del nivel de compra adaptativo y la línea K cruza por encima de la línea D. Entra en corto cuando la línea J está por encima del nivel de venta adaptativo y la línea K cruza por debajo de la línea D.

## Detalles

- **Criterios de entrada**: Cruce KDJ con J por debajo/por encima de niveles dinámicos.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `TimeFrame1` = TimeSpan.FromMinutes(1)
  - `TimeFrame2` = TimeSpan.FromMinutes(3)
  - `TimeFrame3` = TimeSpan.FromMinutes(15)
  - `KdjLength` = 9
  - `SmoothingLength` = 5
  - `TrendLength` = 40
  - `WeightOption` = 1
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Ambos
  - Indicadores: Stochastic, EMA, SMA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
