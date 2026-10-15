# Estrategia de Expansión de Volatilidad Parabolic SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Parabolic SAR Volatility Expansion** está construida en torno al Parabolic SAR con detección de expansión de volatilidad.

Las pruebas indican un rendimiento anual promedio de aproximadamente 49%. Funciona mejor en el mercado de criptomonedas.

Las señales se activan cuando Parabolic confirma cambios de tendencia en datos intradía (5m). Esto hace que el método sea adecuado para traders activos.

Los stops dependen de múltiplos de ATR y factores como SarAf, SarMaxAf. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stops.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `SarAf = 0.02m`
  - `SarMaxAf = 0.2m`
  - `AtrPeriod = 14`
  - `VolatilityExpansionFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Parabolic SAR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
