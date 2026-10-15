# Estrategia Stochastic RSI OHLC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia construye barras OHLC a partir del indicador Stochastic RSI y opera en cambios de momentum. Calcula el RSI para los precios máximo, mínimo y de cierre y aplica un oscilador estocástico a cada serie. Se abre una posición larga cuando el Stochastic RSI sube desde un pivote y cruza por encima del nivel de entrada largo. Se abre una posición corta cuando cae desde un pivote y cruza por debajo del nivel de entrada corto.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Stochastic RSI gira hacia arriba y cualquiera de los últimos tres valores supera `LongEntry` tras un pivote bajo.
  - **Corto**: Stochastic RSI gira hacia abajo y cualquiera de los últimos tres valores cae por debajo de `ShortEntry` tras un pivote alto.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RSI Length` = 14
  - `K Length` = 14
  - `D Length` = 3
  - `LongEntry` = 30
  - `ShortEntry` = 60
  - `LongPivot` = 2
  - `ShortPivot` = 98
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI, Stochastic
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
