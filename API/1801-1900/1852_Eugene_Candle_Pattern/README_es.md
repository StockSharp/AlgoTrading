# Estrategia de Patrón de Velas Eugene
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera un patrón de velas descrito por "Eugene". El algoritmo analiza las últimas cuatro velas, verifica barras interiores y formaciones especiales "pájaro", y calcula niveles de ruptura. Las posiciones se abren en rupturas de los extremos de la vela anterior cuando se cumplen condiciones adicionales de confirmación. Los niveles opcionales de stop loss y take profit se expresan en pasos de precio.

## Detalles

- **Criterios de entrada**:
  - Largo: máximo actual por encima del máximo anterior, mínimo anterior por debajo del máximo anterior anterior, mínimo actual por encima del mínimo anterior, y confirmación por nivel zig o filtro de tiempo.
  - Corto: mínimo actual por debajo del mínimo anterior, máximo anterior por encima del mínimo anterior anterior, máximo actual por debajo del máximo anterior, y confirmación por nivel zig o filtro de tiempo.
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Largo: vender cuando aparece una señal opuesta o se alcanza el stop loss/take profit.
  - Corto: comprar cuando aparece una señal opuesta o se alcanza el stop loss/take profit.
- **Stops**: distancia fija en pasos de precio
- **Valores predeterminados**:
  - `Volume` = 1m
  - `StopLossPoints` = 0
  - `TakeProfitPoints` = 0
  - `InvertSignals` = false
  - `CandleType` = TimeSpan.FromMinutes(1).TimeFrame()
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: Opcional
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: Intradía (filtro hora >= 8)
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
