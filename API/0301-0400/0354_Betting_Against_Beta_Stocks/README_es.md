# Apostar Contra la Beta en Acciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Betting Against Beta Stocks** toma posiciones largas en el decil de menor beta de un universo de acciones y cortas en el decil de mayor beta. El rebalanceo ocurre el primer día de negociación de cada mes.

El enfoque busca explotar la anomalía de que las acciones de baja beta tienden a superar en términos ajustados al riesgo. Se asume acceso a un valor de referencia para los cálculos de beta.

## Detalles
- **Criterios de entrada**: Selección mensual de acciones de baja/alta beta.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Las posiciones se ajustan en el próximo rebalanceo.
- **Stops**: Sin lógica de stop explícita.
- **Valores predeterminados**:
  - `WindowDays = 252`
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `MinTradeUsd = 100`
- **Filtros**:
  - Categoría: Estadístico
  - Dirección: Ambos
  - Indicadores: Beta
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
