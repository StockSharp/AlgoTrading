# Estrategia de Volumen de Compra y Venta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza la distribución del volumen de compra y venta para detectar presión.
Se abre una posición larga cuando el volumen de compra domina y la métrica de volumen
supera una banda de volatilidad mientras el precio está por encima del VWAP semanal. Una posición corta
utiliza las condiciones opuestas.

## Detalles

- **Criterios de entrada**:
  - **Largo**: Volumen de compra ajustado > volumen de venta ajustado, métrica de volumen por encima de la banda superior, close por encima del VWAP semanal.
  - **Corto**: Volumen de venta ajustado > volumen de compra ajustado, métrica de volumen por encima de la banda superior, close por debajo del VWAP semanal.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta o take profit/stop loss basado en ATR.
- **Stops**: Multiplicadores de porcentaje ATR mediante `ProfitTargetLong`, `StopLossLong`, `ProfitTargetShort`, `StopLossShort`.
- **Valores predeterminados**:
  - Length 20, StdDev 2.
  - ProfitTargetLong 100, StopLossLong 1.
  - ProfitTargetShort 100, StopLossShort 5.
- **Filtros**:
  - Categoría: Basado en volumen
  - Dirección: Ambos
  - Indicadores: Personalizado
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Medio plazo
