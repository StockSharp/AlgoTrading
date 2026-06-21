# VWAP EMA ATR Pullback
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de seguimiento de tendencia que utiliza EMA, VWAP y ATR.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 55%. Funciona mejor en el mercado de futuros.

El enfoque identifica tendencias fuertes mediante EMAs rápidas y lentas separadas por una distancia basada en ATR. Las entradas ocurren cuando el precio retrocede hacia el VWAP, con el objetivo de unirse a la tendencia. El take-profit se coloca en el VWAP más o menos el múltiplo del ATR.

## Detalles

- **Criterios de entrada**:
  - **Largo**: tendencia alcista y cierre < VWAP.
  - **Corto**: tendencia bajista y cierre > VWAP.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**: Objetivo en VWAP ± ATR * multiplicador.
- **Stops**: No.
- **Valores predeterminados**:
  - `FastEmaLength` = 30
  - `SlowEmaLength` = 200
  - `AtrLength` = 14
  - `AtrMultiplier` = 1.5
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA, ATR, VWAP
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
