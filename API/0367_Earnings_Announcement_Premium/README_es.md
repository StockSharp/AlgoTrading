# Prima por Anuncio de Resultados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de **Prima por Anuncio de Resultados** compra acciones unos días antes de los anuncios de resultados y sale poco después de la publicación.

## Detalles
- **Criterios de entrada**: Comprar `DaysBefore` días antes de los resultados.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Vender `DaysAfter` días después de los resultados.
- **Stops**: No.
- **Valores predeterminados**:
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Event-driven
  - Dirección: Largo
  - Indicadores: Calendar
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
