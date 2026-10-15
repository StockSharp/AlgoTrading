# Estrategia ICAi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador de media móvil adaptativa ICAi. El indicador suaviza el precio y adapta su pendiente usando la desviación estándar. Las posiciones largas se abren cuando el indicador gira hacia arriba; las posiciones cortas, cuando gira hacia abajo.

El algoritmo funciona en cualquier mercado donde estén disponibles datos de velas. La configuración predeterminada usa un marco temporal de 4 horas y una longitud de suavizado de 12.

## Detalles

- **Criterios de entrada**:
  - Largo: `Prev < PrevPrev && Current >= Prev`
  - Corto: `Prev > PrevPrev && Current <= Prev`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Señal opuesta
- **Stops**: Stop loss y take profit fijos opcionales
- **Valores predeterminados**:
  - `Length` = 12
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: ICAi
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
