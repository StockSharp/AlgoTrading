# Estrategia Exp de Media Móvil FN
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en reversiones de la pendiente de una media móvil exponencial (EMA). Entra en largo cuando la EMA gira hacia arriba tras una caída y entra en corto cuando la EMA gira hacia abajo tras una subida. Los niveles opcionales de stop-loss y take-profit se definen en unidades de precio absoluto.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La pendiente de la EMA cambia de bajista a alcista.
  - **Corto**: La pendiente de la EMA cambia de alcista a bajista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**:
  - Reversión de pendiente opuesta.
  - Activación del stop-loss o take-profit.
- **Stops**: Sí, usando distancias de precio absoluto.
- **Valores predeterminados**:
  - `EMA Length` = 12
  - `Stop Loss` = 1000
  - `Take Profit` = 2000
  - `Candle Type` = marco temporal de 4 horas
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Único (EMA)
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Moderado
