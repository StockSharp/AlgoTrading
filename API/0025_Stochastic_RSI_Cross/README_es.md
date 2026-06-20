# Stochastic RSI Cross
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el cruce del Stochastic RSI

Las pruebas indican un rendimiento anual promedio de aproximadamente 112%. Funciona mejor en el mercado forex.

Stochastic RSI Cross observa las líneas %K y %D del StochRSI. Los cruces alcistas cerca de niveles de sobreventa desencadenan compras, los cruces bajistas cerca de la sobrecompra desencadenan ventas, y los cruces opuestos salen.

Dado que el StochRSI oscila rápidamente, las señales pueden ser frecuentes. Muchos traders requieren que el cruce ocurra cerca de un extremo para filtrar el ruido.


## Detalles

- **Criterios de entrada**: Señales basadas en RSI, Stochastic.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `StochPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: RSI, Stochastic
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

