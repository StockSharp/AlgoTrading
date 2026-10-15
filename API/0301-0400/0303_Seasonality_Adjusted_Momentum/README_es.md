# Estrategia de Momentum Ajustado por Estacionalidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **Seasonality Adjusted Momentum** está construida alrededor del indicador de momentum ajustado con la fuerza de la estacionalidad.

Las pruebas indican un retorno anual promedio de aproximadamente el 172%. Funciona mejor en el mercado de divisas.

Se ejecuta sobre **velas terminadas de 5 minutos**, no diarias. `M = Momentum(14)`, `A = SMA(14) de M`, `s = score[candle.OpenTime.Month]`. Desde plano, comprar si `s >= 0.5` y `M > A`; vender si `s <= -0.5` y `M < A`. Cerrar largo si `s < 0.5` o `M < A`; corto si `s > -0.5` o `M > A`. Espera de 120 velas tras una orden. La protección es un **stop nativo del 2%**, no ATR.

Valores de s de enero a diciembre: `0.8, 0.2, 0.5, 0.7, 0.3, -0.2, -0.3, -0.4, -0.7, 0.4, 0.6, 0.9`. BTC/TON solo cubre marzo (s=0.5): permite largos pero no nuevos cortos. El retorno del 172% **no está verificado**.

## Detalles
- **Criterios de entrada**: ver implementación para las condiciones del indicador.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: señal opuesta o lógica de stop.
- **Stops**: Sí, usando cálculos basados en indicadores.
- **Valores predeterminados**:
  - `MomentumPeriod = 14`
  - `SeasonalityThreshold = 0.5m`
  - `StopLossPercent = 2.0m`
  - `CandleType = TimeSpan.FromMinutes(5).TimeFrame()`
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Seasonality, Adjusted
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
