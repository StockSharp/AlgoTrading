# Estrategia de Rompimiento de Nube Ichimoku Solo Largos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia abre posiciones largas cuando el precio rompe por encima de la nube Ichimoku y sale cuando el precio cae de nuevo por debajo de ella. Solo se realizan operaciones largas.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close` cruza por encima de `max(SenkouA, SenkouB)`
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - `Close` cruza por debajo de `min(SenkouA, SenkouB)`
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `TenkanPeriod` = 9
  - `KijunPeriod` = 26
  - `SenkouSpanBPeriod` = 52
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: Ichimoku
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
