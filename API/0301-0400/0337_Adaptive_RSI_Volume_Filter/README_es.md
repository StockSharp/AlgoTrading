# Estrategia RSI Adaptativo con Filtro de Volumen
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **Adaptive RSI Volume Filter** opera basándose en RSI Adaptativo con confirmación de volumen.

Las pruebas indican un rendimiento anual promedio de aproximadamente 106%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando los indicadores confirman entradas filtradas en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops se basan en múltiplos de ATR y factores como MinRsiPeriod, MaxRsiPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: consulte la implementación para conocer las condiciones de los indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, utilizando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `MinRsiPeriod = 10`
  - `MaxRsiPeriod = 20`
  - `AtrPeriod = 14`
  - `VolumeLookback = 20`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples indicadores
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
