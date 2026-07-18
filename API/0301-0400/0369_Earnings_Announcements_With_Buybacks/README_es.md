# Anuncios de Resultados con Recompras de Acciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de **Anuncios de Resultados con Recompras de Acciones** compra acciones con programas de recompra activos unos días antes de sus anuncios de resultados y sale poco después del informe.

## Detalles
- **Criterios de entrada**: Comprar `DaysBefore` días antes de los resultados si la empresa tiene una recompra activa.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Vender `DaysAfter` días después de la fecha de resultados.
- **Stops**: No.
- **Valores predeterminados**:
  - `DaysBefore = 5`
  - `DaysAfter = 1`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Event-driven
  - Dirección: Largo
  - Indicadores: Buyback + Calendar
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
