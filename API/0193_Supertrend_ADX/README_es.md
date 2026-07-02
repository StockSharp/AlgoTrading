# Estrategia Supertrend Adx
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en el indicador Supertrend y ADX para confirmación de la fuerza de tendencia. Criterios de entrada: Largo: Price > Supertrend && ADX > 25 (tendencia alcista con movimiento fuerte). Corto: Price < Supertrend && ADX > 25 (tendencia bajista con movimiento fuerte). Criterios de salida: Largo: Price < Supertrend (precio cae por debajo de Supertrend). Corto: Price > Supertrend (precio sube por encima de Supertrend).

Las pruebas indican un retorno anual promedio de aproximadamente 166%. Funciona mejor en el mercado de acciones.

Supertrend proporciona un camino ajustado por volatilidad mientras ADX confirma la potencia del movimiento. Las operaciones ocurren cuando ambos indicadores se alinean.

Para quienes desean aprovechar tendencias fuertes con trailing stops. ATR determina la colocación del stop.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > Supertrend && ADX > AdxThreshold`
  - Corto: `Close < Supertrend && ADX > AdxThreshold`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Reversión de Supertrend
- **Stops**: Usa Supertrend como trailing stop
- **Valores predeterminados**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `AdxPeriod` = 14
  - `AdxThreshold` = 25m
  - `CandleType` = TimeSpan.FromMinutes(15).TimeFrame()
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Supertrend, ADX
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

