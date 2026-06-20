# Supertrend Stochastic Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia Supertrend + Stochastic. La estrategia entra en operaciones cuando Supertrend indica la dirección de la tendencia y Stochastic confirma con condiciones de sobrevendido/sobrecomprado.

Las pruebas indican un retorno anual promedio de aproximadamente 142%. Funciona mejor en el mercado de acciones.

Supertrend marca la tendencia y Stochastic señala movimientos contrarios temporales. Las entradas ocurren una vez que Stochastic sale de sobrevendido o sobrecomprado en contra de la tendencia.

Ideal para traders de momentum que necesitan señales de tendencia claras. Los valores de ATR definen la distancia del stop.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > Supertrend && StochK < 20`
  - Corto: `Close < Supertrend && StochK > 80`
- **Largo/Corto**: Ambos
- **Criterios de salida**: Reversión de Supertrend
- **Stops**: Usa Supertrend como trailing stop
- **Valores predeterminados**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Supertrend, Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

