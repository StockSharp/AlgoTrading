# Estrategia Color Bulls Gap
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que recrea el indicador ColorBullsGap comparando brechas suavizadas entre el precio máximo y los promedios de apertura y cierre.
Entra largo cuando el color hace dos barras era alcista y se vuelve neutral o bajista en la última barra, cerrando cualquier posición corta.
Entra corto cuando el color hace dos barras era bajista y se vuelve neutral o alcista en la última barra, cerrando cualquier posición larga.

## Detalles

- **Criterios de entrada**:
  - Largo: `PrevColor == 0 && LastColor > 0`
  - Corto: `PrevColor == 2 && LastColor < 2`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Length1` = 12
  - `Length2` = 5
  - `CandleType` = TimeSpan.FromHours(8).TimeFrame()
- **Filtros**:
  - Categoría: Indicador
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
