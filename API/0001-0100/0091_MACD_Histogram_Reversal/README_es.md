# Estrategia de Reversión del Histograma MACD
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El histograma MACD representa la diferencia entre la línea MACD y su línea de señal. Los cruces por encima o por debajo de cero suelen marcar cambios de momentum. Esta estrategia opera esos cruces de la línea cero y gestiona el riesgo con un stop porcentual.

Las pruebas indican una rentabilidad anual media de aproximadamente el 160%. Funciona mejor en el mercado de divisas.

En cada vela se calcula el histograma MACD. Cuando pasa de negativo a positivo, se abre una posición larga. Un cambio de positivo a negativo activa una venta corta. Dado que la estrategia solo busca el cruce de cero, las operaciones son directas y típicamente de corto plazo.

Los stops se usan para contener las pérdidas si el momentum no continúa en la dirección esperada.

## Detalles

- **Criterios de entrada**: El histograma MACD cruza el cero.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss.
- **Stops**: Sí, basado en porcentaje.
- **Valores predeterminados**:
  - `FastPeriod` = 12
  - `SlowPeriod` = 26
  - `SignalPeriod` = 9
  - `StopLoss` = 2%
  - `CandleType` = 15 minute
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: MACD
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

