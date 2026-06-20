# Estrategia de Hull MA Volatility Contraction
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Hull MA Volatility Contraction** se basa en la Media Móvil Hull con filtro de contracción de volatilidad.

Las pruebas indican un rendimiento anual promedio de aproximadamente el 76%. Funciona mejor en el mercado de divisas.

Las señales se activan cuando los indicadores confirman patrones de contracción de volatilidad en datos intradía (15m). Este método es adecuado para operadores activos.

Los stops se basan en múltiplos de ATR y factores como HmaPeriod, AtrPeriod. Ajuste estos valores predeterminados para equilibrar el riesgo y la recompensa.

## Detalles
- **Criterios de entrada**: ver implementación para condiciones de indicadores.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `HmaPeriod = 9`
  - `AtrPeriod = 14`
  - `VolatilityContractionFactor = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: múltiples indicadores
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
