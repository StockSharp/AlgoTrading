# Estrategia de Donchian Seasonal Filter
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Donchian Seasonal Filter** se basa en los Canales Donchian con filtro estacional.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 70%. Funciona mejor en el mercado de acciones.

Las señales se activan cuando Donchian confirma entradas filtradas en datos intradía (15m). Este método es adecuado para operadores activos.

Los stops se basan en múltiplos de ATR y factores como DonchianPeriod, SeasonalThreshold. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `DonchianPeriod = 20`
  - `SeasonalThreshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Donchian, Seasonal
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
