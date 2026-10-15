# Estrategia Color Step Xccx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el indicador Color Step XCCX. El indicador mide la desviación del precio respecto a una media suavizada y traza dos líneas escalonadas. Se abre una operación larga cuando la línea rápida cae por debajo de la línea lenta. Se abre una operación corta cuando la línea rápida sube por encima de la línea lenta.

## Detalles

- **Criterios de entrada**:
  - Largo: la línea rápida cruza por debajo de la línea lenta
  - Corto: la línea rápida cruza por encima de la línea lenta
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: la línea rápida cruza por encima de la línea lenta
  - Corto: la línea rápida cruza por debajo de la línea lenta
- **Stops**: Ninguno
- **Valores predeterminados**:
  - `DPeriod` = 30
  - `MPeriod` = 7
  - `StepSizeFast` = 5
  - `StepSizeSlow` = 30
  - `CandleType` = TimeSpan.FromHours(4).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Custom, EMA
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
