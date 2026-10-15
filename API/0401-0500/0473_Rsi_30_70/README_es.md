# Estrategia RSI 30-70
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta sencilla estrategia de momentum utiliza el Índice de Fuerza Relativa (RSI) para identificar zonas de sobrecompra y sobreventa. Cuando el RSI cae por debajo del nivel de sobreventa, se abre una posición larga. La operación se cierra una vez que el RSI sube por encima del umbral de sobrecompra. El sistema opera en un único marco temporal y no toma posiciones cortas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `RSI < oversold`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - **Largo**: `RSI > overbought`.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RSI Length` = 14.
  - `Overbought/Oversold` = 70 / 30.
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Long
  - Indicadores: Único
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
