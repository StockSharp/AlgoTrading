# Estrategia de Asimetría de Retorno en Materias Primas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Asimetría de Retorno en Materias Primas** explota la diferencia entre retornos positivos y negativos. Para cada futuro de materias primas, la ventana deslizante suma por separado todos los movimientos alcistas y bajistas. Un ratio alto implica una tendencia positiva persistente, mientras que un ratio bajo señala presión vendedora sostenida.

Al inicio de cada mes, las materias primas se clasifican por esta medida de asimetría. El sistema compra los N contratos superiores y vende en corto los N más débiles, asignando capital de forma equitativa. El rebalanceo ocurre mensualmente.

## Detalles
- **Criterios de entrada**: Clasificación mensual de la asimetría de los retornos diarios en una ventana de retroceso.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Posiciones ajustadas en el rebalanceo mensual.
- **Stops**: Sin stop explícito; tamaño de posición limitado por `MinTradeUsd`.
- **Valores predeterminados**:
  - `WindowDays = 120`
  - `TopN = 5`
  - `MinTradeUsd = 200`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Basados en precio
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
