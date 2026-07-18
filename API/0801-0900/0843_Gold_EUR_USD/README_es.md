# Estrategia de Captura de Liquidez en Gold y EUR/USD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia detecta capturas de liquidez en zonas de oferta y demanda en Gold y EUR/USD usando RSI, SMA, Oscilador Estocástico y brechas de valor razonable basadas en ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: El precio pincha por debajo del mínimo reciente, la estructura de mercado se desplaza hacia arriba, se produce una brecha de valor razonable, RSI sobrevendido, precio por encima de la SMA, Estocástico sobrevendido.
  - **Corto**: El precio pincha por encima del máximo reciente, la estructura de mercado se desplaza hacia abajo, se produce una brecha de valor razonable, RSI sobrecomprado, precio por debajo de la SMA, Estocástico sobrecomprado.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal inversa.
- **Stops**: No.
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `MaLength` = 50
  - `StochLength` = 14
  - `Overbought` = 70
  - `Oversold` = 30
  - `StochOverbought` = 80
  - `StochOversold` = 20
- **Filtros**:
  - Categoría: Price action
  - Dirección: Ambos
  - Indicadores: RSI, SMA, Stochastic, ATR, Highest, Lowest
  - Stops: No
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
