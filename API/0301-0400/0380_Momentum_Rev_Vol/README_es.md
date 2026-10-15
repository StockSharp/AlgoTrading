# Estrategia de Momentum, Reversión y Volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de factores compuestos combina tres señales: momentum a largo plazo,
reversión a corto plazo y baja volatilidad. Cada mes se calcula una puntuación para
cada valor usando el momentum de 12 meses, el inverso de los retornos de un mes y la
volatilidad de los últimos 60 días. Los pesos ajustables `WM`, `WR` y `WV` controlan
la contribución de cada componente.

En el primer día de negociación de cada mes, los valores se clasifican por puntuación
compuesta. El decil más alto se compra y el decil más bajo se vende en corto con pesos
iguales en dólares. Las posiciones se mantienen hasta el siguiente rebalanceo y no se
emplean reglas explícitas de stop-loss.

Al combinar seguimiento de tendencia, reversión a la media y aversión al riesgo, la
estrategia busca retornos diversificados en diferentes regímenes de mercado.

## Detalles

- **Criterios de entrada**: Clasificación mensual por combinación ponderada de momentum,
  reversión y volatilidad; largo en el decil superior, corto en el decil inferior
- **Largo/Corto**: Ambos
- **Criterios de salida**: Siguiente rebalanceo mensual
- **Stops**: No
- **Valores predeterminados**:
  - `Lookback12` = 252
  - `Lookback1` = 21
  - `VolWindow` = 60
  - `WM` = 1.0
  - `WR` = 1.0
  - `WV` = 1.0
  - `MinTradeUsd` = 200
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Multi-factor
  - Dirección: Ambos
  - Indicadores: Momentum, reversión, volatilidad
  - Stops: No
  - Complejidad: Avanzado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
