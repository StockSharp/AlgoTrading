# Estrategia de Momentum Squeeze de Ruptura BB
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia BB Breakout Momentum Squeeze combina un oscilador de ruptura de Bollinger Bands con un filtro de squeeze de volatilidad. El squeeze se detecta cuando las Bollinger Bands se mueven fuera de los Keltner Channels, señalando una posible expansión. Una operación larga ocurre cuando el oscilador alcista cruza por encima del umbral durante esta expansión, mientras que una operación corta usa el cruce bajista. Los stops se basan en una banda ATR y un objetivo de riesgo-recompensa completa la lógica de salida.

## Detalles

- **Criterios de entrada**:
  - Squeeze apagado (Bollinger Bands fuera de los Keltner Channels).
  - **Largo**: el oscilador alcista cruza por encima del umbral.
  - **Corto**: el oscilador bajista cruza por encima del umbral.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - El precio alcanza el stop ATR o el objetivo de riesgo-recompensa.
- **Stops**: Banda ATR con objetivo de riesgo-recompensa.
- **Valores predeterminados**:
  - `BbLength` = 14
  - `BbMultiplier` = 1.0
  - `Threshold` = 50
  - `SqueezeLength` = 20
  - `SqueezeBbMultiplier` = 2.0
  - `KcMultiplier` = 2.0
  - `AtrLength` = 30
  - `AtrMultiplier` = 1.4
  - `RrRatio` = 1.5
- **Filtros**:
  - Categoría: Ruptura de volatilidad
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Keltner Channels, ATR
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
