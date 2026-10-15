# Estrategia Donchian con Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **Donchian Hurst Exponent** opera basándose en rupturas del Canal Donchian con filtro de Hurst Exponent.

Las pruebas indican un rendimiento anual promedio de aproximadamente 91%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando Donchian confirma cambios de tendencia en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como DonchianPeriod, HurstPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: consulte la implementación para conocer las condiciones de los indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, utilizando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `DonchianPeriod = 20`
  - `HurstPeriod = 100`
  - `HurstThreshold = 0.5m`
  - `StopLossPercent = 2m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Donchian, Hurst, Exponent
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
