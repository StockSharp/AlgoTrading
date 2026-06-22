# Estrategia Exp Super Trend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia convertida del script MQL **Exp_Super_Trend.mq5** (ID 14269). Sigue la dirección del indicador SuperTrend e invierte las posiciones cada vez que la tendencia cambia. La implementación utiliza la API de alto nivel de StockSharp y el indicador SuperTrend integrado.

El indicador calcula una línea dinámica de soporte o resistencia basada en ATR. Cuando el precio se mantiene por encima de esta línea, la tendencia se considera alcista; de lo contrario, bajista. La estrategia abre una posición larga durante los períodos alcistas y cambia a una posición corta durante los períodos bajistas. Cada cambio del indicador provoca una inversión inmediata de la posición.

Este enfoque funciona mejor en mercados tendenciales donde los grandes movimientos siguen a una ruptura. También es útil como plantilla educativa que muestra cómo conectar un indicador usando `BindEx` y ejecutar órdenes de mercado en velas completadas.

## Detalles

- **Criterios de entrada**:
  - Largo: SuperTrend señala una tendencia alcista.
  - Corto: SuperTrend señala una tendencia bajista.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta de SuperTrend (la posición se invierte).
- **Stops**: Sin stop loss explícito; la línea del indicador actúa como trailing stop.
- **Valores predeterminados**:
  - `AtrPeriod` = 10
  - `Multiplier` = 3m
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: SuperTrend
  - Stops: Basado en indicador
  - Complejidad: Básico
  - Marco temporal: Medio (1 hora por defecto)
  - Estacionalidad: Ninguno
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
