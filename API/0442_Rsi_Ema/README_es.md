# Estrategia de Tendencia RSI + EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este sistema combina un oscilador clásico de Relative Strength Index (RSI) con un filtro de tendencia de doble media móvil. El RSI proporciona lecturas de corto plazo de sobrecompra y sobreventa mientras las dos medias móviles exponenciales (EMAs) definen la tendencia más amplia. La estrategia solo toma operaciones en la dirección de la EMA rápida relativa a la EMA lenta, ayudando a evitar configuraciones contratendencia durante movimientos direccionales fuertes.

Cuando el momentum del precio empuja el RSI por debajo del umbral de sobreventa y la EMA rápida está por encima de la EMA lenta, se asume que el mercado está en tendencia alcista y se abre una posición larga. A la inversa, si RSI sube por encima del nivel de sobrecompra mientras la EMA rápida aún supera la EMA lenta, la estrategia inicia una operación corta, esperando un retroceso de corto plazo dentro del canal de tendencia mayor.

Las posiciones se cierran cuando RSI sale de la zona extrema hacia el lado opuesto, señalando que el movimiento de reversión a la media probablemente se haya agotado. El método es simple pero efectivo para capturar breves oscilaciones de momentum en entornos de tendencia. Funciona bien en instrumentos líquidos donde los extremos de RSI ocurren frecuentemente pero la dirección de la tendencia permanece intacta.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `RSI < oversold` y `EMA1 > EMA2`.
  - **Corto**: `RSI > overbought` y `EMA1 > EMA2`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: `RSI > overbought`.
  - **Corto**: `RSI < oversold`.
- **Stops**: Ninguno integrado.
- **Valores predeterminados**:
  - `RSI Length` = 14.
  - `Overbought/Oversold` = 70 / 30.
  - `EMA Lengths` = 150 / 600.
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
