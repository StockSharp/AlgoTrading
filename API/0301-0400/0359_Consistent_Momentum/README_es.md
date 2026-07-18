# Estrategia de Momentum Consistente
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Consistent Momentum** selecciona instrumentos que exhiben un momentum fuerte en dos ventanas temporales y rebalancea el portafolio mensualmente. Mantiene cada tramo durante un número fijo de meses y asigna capital en partes iguales a las cestas largas y cortas.

## Detalles
- **Criterios de entrada**: En el primer día de negociación de cada mes, tomar posiciones largas en los valores del decil superior de ambas medidas de momentum y cortas en el decil inferior.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Las posiciones se cierran al expirar el período de tenencia o cuando ocurre el rebalanceo.
- **Stops**: Sin lógica de stop explícita; el tamaño de posición se basa en la asignación en dólares.
- **Valores predeterminados**:
  - `LookbackDays = 7 * 21`
  - `HoldingMonths = 6`
  - `MinTradeUsd = 50`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Price momentum
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
