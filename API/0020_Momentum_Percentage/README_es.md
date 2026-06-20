# Momentum Percentage
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Estrategia basada en el cambio porcentual del momentum de precio

Las pruebas indican un retorno anual promedio de aproximadamente 97%. Funciona mejor en el mercado de criptomonedas.

Momentum Percentage rastrea el cambio porcentual en el precio. Las operaciones se activan cuando el momentum supera los niveles positivos o negativos y salen con la señal contraria o un stop de volatilidad.

Al medir los retornos a lo largo de un período de referencia establecido, el sistema se adapta a diferentes mercados. El stop de volatilidad garantiza que los movimientos adversos grandes salgan rápidamente.


## Detalles

- **Criterios de entrada**: Señales basadas en MA, Momentum.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MomentumPeriod` = 10
  - `ThresholdPercent` = 5m
  - `StopLossPercent` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA, Momentum
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Neural Networks: No
  - Divergencia: No
  - Nivel de riesgo: Medio

