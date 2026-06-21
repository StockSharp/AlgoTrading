# Estrategia TSI DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que calcula el True Strength Index sobre el oscilador DeMarker.
Se abre una posición larga cuando el TSI cruza por encima de su línea de señal de media móvil.
Se abre una posición corta cuando el TSI cruza por debajo de la línea de señal.

El enfoque combina análisis de momentum y de sobrecompra/sobreventa.

## Detalles

- **Criterios de entrada**:
  - Largo: `TSI cruza por encima de la señal`
  - Corto: `TSI cruza por debajo de la señal`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
  - `DemarkerPeriod` = 25
  - `ShortLength` = 5
  - `LongLength` = 8
  - `SignalLength` = 20
- **Filtros**:
  - Categoría: Oscilador cruce
  - Dirección: Ambos
  - Indicadores: TSI, DeMarker
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
