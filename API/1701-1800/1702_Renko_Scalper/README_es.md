# Estrategia de Scalping Renko
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia intenta capturar momentum a corto plazo comparando el cierre actual con el cierre anterior.
Si la última vela cierra más alto que la anterior, la estrategia abre una posición larga.
Si la última vela cierra más bajo que la anterior, abre una posición corta.

Los stops y el trailing stop opcional son gestionados a través del módulo de protección integrado.
El enfoque funciona en ambos lados del mercado y se basa únicamente en la acción del precio.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close(t) > Close(t-1)`.
  - **Corto**: `Close(t) < Close(t-1)`.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o stops de protección.
- **Stops**: Trailing stop, stop loss y take profit opcionales mediante `StartProtection`.
- **Valores predeterminados**:
  - `CandleType` = 1 minuto.
  - `StopLossPercent` = 1.
  - `TakeProfitPercent` = 2.
  - `IsTrailingStop` = true.
- **Filtros**:
  - Categoría: Scalping.
  - Dirección: Ambos.
  - Indicadores: Ninguno.
  - Stops: Sí.
  - Complejidad: Simple.
  - Marco temporal: Corto plazo.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Alto.
