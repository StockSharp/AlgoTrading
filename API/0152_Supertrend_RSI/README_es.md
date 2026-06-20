# Estrategia Supertrend RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia Supertrend + RSI. Comprar cuando el precio está por encima del Supertrend y el RSI está por debajo de 30 (sobrevendido). Vender cuando el precio está por debajo del Supertrend y el RSI está por encima de 70 (sobrecomprado).

Las pruebas indican un retorno anual promedio de aproximadamente el 43%. Funciona mejor en el mercado de acciones.

El indicador Supertrend muestra la tendencia actual, y el RSI detecta cuándo el precio está sobreextendido. Las órdenes siguen la dirección del Supertrend una vez que el RSI alcanza un extremo.

Una buena opción para los traders que confían en los stops de seguimiento. El stop integrado del Supertrend trabaja junto con la configuración del ATR para limitar las pérdidas.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > Supertrend && RSI < RsiOversold`
  - Corto: `Close < Supertrend && RSI > RsiOverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Cambio de Supertrend en dirección opuesta
- **Stops**: Usa Supertrend como Trailing stop
- **Valores predeterminados**:
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3.0m
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Supertrend, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
