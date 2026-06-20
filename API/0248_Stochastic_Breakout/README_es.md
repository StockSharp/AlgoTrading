# Estrategia de Ruptura Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este enfoque de ruptura monitorea el oscilador Stochastic en busca de movimientos bruscos alejados de su promedio reciente. Cuando la línea %K rompe por encima o por debajo de un umbral ajustado por volatilidad, señala un estallido de momentum que puede iniciar una tendencia.

Las pruebas indican un rendimiento anual promedio de aproximadamente 181%. Funciona mejor en el mercado de criptomonedas.

Una posición larga se activa cuando %K cruza por encima del umbral superior tras un período de contracción. Se toma una posición corta cuando %K rompe por debajo del umbral inferior. La operación se cierra cuando el oscilador se deriva de vuelta hacia su promedio o alcanza un stop protector.

La estrategia está diseñada para traders intradía que quieren entrar temprano en oscilaciones de momentum. El uso de bandas basadas en volatilidad ayuda a filtrar el ruido para que solo los movimientos decisivos generen señales.

## Detalles
- **Criterios de entrada**:
  - **Largo**: %K > Avg + DeviationMultiplier * StdDev
  - **Corto**: %K < Avg - DeviationMultiplier * StdDev
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir cuando %K < Avg
  - **Corto**: Salir cuando %K > Avg
- **Stops**: Sí, stop-loss porcentual.
- **Valores predeterminados**:
  - `StochasticPeriod` = 14
  - `KPeriod` = 3
  - `DPeriod` = 3
  - `LookbackPeriod` = 20
  - `DeviationMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Stochastic Oscillator
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
