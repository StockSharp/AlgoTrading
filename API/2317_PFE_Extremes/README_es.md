# PFE Extremos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera rupturas del indicador Polarized Fractal Efficiency (PFE). Cuando el PFE cruza por encima del nivel superior, la estrategia cierra cualquier posición corta y abre una larga. Cuando el PFE cruza por debajo del nivel inferior, cierra posiciones largas y abre una corta.

El indicador PFE evalúa qué tan eficientemente se mueve el precio en relación con su trayectoria. Los valores cercanos a +1 sugieren un movimiento ascendente fuerte, mientras que los valores cercanos a -1 muestran un movimiento descendente fuerte. Los cruces de umbral pueden resaltar el inicio de una nueva tendencia.

## Detalles

- **Criterios de entrada**: PFE cruza por encima de `UpLevel` para largos o por debajo de `DownLevel` para cortos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Ruptura del nivel opuesto o señal de reversión.
- **Stops**: No se usan por defecto; se pueden agregar mediante protección de posición.
- **Valores predeterminados**:
  - `PfePeriod` = 5
  - `UpLevel` = 0.5
  - `DownLevel` = -0.5
  - `CandleType` = marco temporal de 4 horas
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: PFE
  - Stops: Opcional
  - Complejidad: Básico
  - Marco temporal: Swing
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
