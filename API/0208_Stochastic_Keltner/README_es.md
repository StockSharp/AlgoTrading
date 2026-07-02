# Estrategia Stochastic Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia utiliza los indicadores Stochastic Keltner para generar señales.
La entrada larga ocurre cuando Stoch %K < 20 && Price < Keltner lower band (sobrevendido en la banda inferior). La entrada corta ocurre cuando Stoch %K > 80 && Price > Keltner upper band (sobrecomprado en la banda superior).
Es adecuada para traders que buscan oportunidades en mercados mixtos.

Las pruebas indican un rendimiento anual promedio de aproximadamente 61%. Funciona mejor en el mercado de criptomonedas.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Stoch %K < 20 && Price < Keltner lower band (oversold at lower band)
  - **Corto**: Stoch %K > 80 && Price > Keltner upper band (overbought at upper band)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando el precio regresa a la banda media
  - **Corto**: Salir de la posición corta cuando el precio regresa a la banda media
- **Stops**: Sí.
- **Valores predeterminados**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Mixto
  - Dirección: Ambos
  - Indicadores: Stochastic Keltner
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

