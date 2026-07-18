# Estrategia de Apuesta Contra la Beta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Betting Against Beta** toma posiciones largas en los activos de menor beta y cortas en los de mayor beta. Las betas se
calculan frente a un índice de referencia en una ventana deslizante y el portafolio se rebalancea el primer día de negociación de cada
mes.

## Detalles
- **Criterios de entrada**: clasificar el universo por beta relativa al índice de referencia; largo en el decil más bajo, corto en el más alto.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Posiciones ajustadas en el próximo rebalanceo mensual.
- **Stops**: Sin lógica de stop explícita.
- **Valores predeterminados**:
  - `WindowDays = 252`
  - `Deciles = 10`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
  - `MinTradeUsd = 100`
- **Filtros**:
  - Categoría: Factor
  - Dirección: Ambos
  - Indicadores: Estadístico
  - Stops: No
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
