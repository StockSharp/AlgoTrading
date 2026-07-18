# Estrategia de Ichimoku Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Ichimoku Hurst Exponent** se basa en el indicador Ichimoku Kinko Hyo con filtro de exponente Hurst.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 64%. Funciona mejor en el mercado de divisas.

Las señales se activan cuando Hurst confirma cambios de tendencia en datos intradía (15m). Este método es adecuado para operadores activos.

Los stops se basan en múltiplos de ATR y factores como TenkanPeriod, KijunPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `TenkanPeriod = 9`
  - `KijunPeriod = 26`
  - `SenkouSpanBPeriod = 52`
  - `HurstPeriod = 100`
  - `HurstThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Hurst, Exponent
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
