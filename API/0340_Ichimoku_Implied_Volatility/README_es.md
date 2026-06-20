# Estrategia Ichimoku de Volatilidad Implícita
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La estrategia **Ichimoku Implied Volatility** está construida en torno a la volatilidad implícita de Ichimoku.

Las pruebas indican un retorno anual promedio de aproximadamente 109%. Funciona mejor en el mercado de criptomonedas.

Las señales se activan cuando sus indicadores confirman cambios de tendencia en datos intradía (15m). Esto hace que el método sea adecuado para traders activos.

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
  - `IVPeriod = 20`
  - `CandleType = TimeSpan.FromMinutes(15).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples indicadores
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía (15m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

