# Estrategia Delta SMA Máximo-Mínimo de 1 Año
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Delta SMA Máximo-Mínimo de 1 Año** calcula el delta de volumen (volumen comprador menos vendedor) y su media móvil simple. Entra en largo cuando el delta SMA estuvo muy bajo y cruza por encima de cero. La posición se cierra cuando el delta SMA cae por debajo del 60% de su máximo de 1 año después de haber cruzado previamente el 70% de ese máximo.

## Detalles
- **Criterios de entrada**: El delta SMA estaba por debajo del 70% de su mínimo de 1 año y cruza por encima de cero.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: El delta SMA cae por debajo del 60% de su máximo de 1 año después de cruzar el 70%.
- **Stops**: No.
- **Valores predeterminados**:
  - `DeltaSmaLength = 14`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Volumen
  - Dirección: Largo
  - Indicadores: SMA, Highest, Lowest
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
