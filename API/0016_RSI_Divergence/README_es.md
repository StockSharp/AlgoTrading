# RSI Divergence
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en la divergencia del RSI

Las pruebas indican un retorno anual promedio de aproximadamente 85%. Funciona mejor en el mercado de criptomonedas.

RSI Divergence busca extremos de precio no confirmados por el oscilador RSI. Una divergencia alcista lleva a una compra y una divergencia bajista provoca una venta. La operación dura hasta que el RSI revierte o se activa un stop.

Las configuraciones de divergencia suelen aparecer cerca del final de tendencias largas. Al comparar el comportamiento del oscilador con la acción del precio, la estrategia intenta capturar reversiones tempranas con riesgo controlado.


## Detalles

- **Criterios de entrada**: Señales basadas en RSI.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Neural Networks: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio

