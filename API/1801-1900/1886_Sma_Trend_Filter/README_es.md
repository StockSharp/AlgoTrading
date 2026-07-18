# Estrategia de Filtro de Tendencia SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia multitemporal que analiza la pendiente de cinco medias móviles simples (períodos 5, 8, 13, 21, 34) en tres marcos temporales (15m, 1h, 4h). Calcula puntuaciones alcistas y bajistas para cada marco temporal y opera cuando todos se alinean en una dirección.

## Detalles

- **Criterios de entrada**:
  - Largo: los tres marcos temporales muestran al menos el 50% de las SMA subiendo
  - Corto: los tres marcos temporales muestran al menos el 50% de las SMA bajando
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta basada en el nivel de cierre
- **Stops**: No
- **Valores predeterminados**:
  - `OpenLevel` = 0
  - `CloseLevel` = 0
  - `CandleType1` = TimeSpan.FromMinutes(15).TimeFrame()
  - `CandleType2` = TimeSpan.FromHours(1).TimeFrame()
  - `CandleType3` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Multitemporal
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
