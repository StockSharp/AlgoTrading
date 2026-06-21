# Estrategia de Histograma XKRI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el Kairi Relative Index (KRI) suavizado por una media móvil exponencial. El sistema busca mínimos y máximos locales del oscilador suavizado y entra en posiciones largas o cortas cuando aparece un patrón de reversión.

## Detalles

- **Criterios de entrada**:
  - Largo: `Kri[1] < Kri[2] && Kri[0] > Kri[1]`
  - Corto: `Kri[1] > Kri[2] && Kri[0] < Kri[1]`
- **Largo/Corto**: Ambos
- **Stops**: Take profit y stop loss en puntos
- **Valores predeterminados**:
  - `KriPeriod` = 20
  - `SmoothPeriod` = 7
  - `TakeProfit` = 2000
  - `StopLoss` = 1000
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Reversión
  - Dirección: Ambos
  - Indicadores: Kairi, EMA
  - Stops: Sí
  - Complejidad: Principiante
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
