# Estrategia Stochastic RSI SuperTrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este sistema combina las oscilaciones rápidas del Stochastic RSI con un filtro de
tendencia y un modelo SuperTrend simplificado. El oscilador resalta los extremos de
momentum a corto plazo, mientras que la media móvil y las bandas ATR definen la
tendencia dominante. Las operaciones se abren solo cuando la línea %K cruza %D dentro
de la zona relevante y la tendencia más amplia está alineada, reduciendo las señales
falsas en condiciones laterales.

La configuración predeterminada se centra en operaciones largas, pero opcionalmente
puede habilitar entradas cortas. La estrategia está diseñada para marcos temporales
intradía o swing, donde las señales del Stochastic RSI aparecen con frecuencia y las
bandas basadas en ATR proporcionan un sesgo adaptativo a la volatilidad. Las salidas
ocurren en cruces opuestos, permitiendo que el mercado corra hasta que el momentum
se debilite.

## Detalles

- **Criterios de entrada**:
  - **Largo**: cierre por encima de la MA de tendencia, %K < 20, %K cruza por encima de %D, SuperTrend muestra tendencia alcista.
  - **Corto**: cierre por debajo de la MA de tendencia, %K > 80, %K cruza por debajo de %D, SuperTrend muestra tendencia bajista.
- **Largo/Corto**: Largo por defecto, corto opcional.
- **Criterios de salida**:
  - **Largo**: %K > 80 y cruza por debajo de %D.
  - **Corto**: %K < 20 y cruza por encima de %D.
- **Stops**: Ninguno por defecto; se pueden añadir externamente.
- **Valores predeterminados**:
  - Período RSI = 14, longitud Stochastic = 14.
  - Tipo MA = EMA, longitud MA = 100.
  - Período ATR = 10, factor ATR = 3.0.
- **Filtros**:
  - Categoría: Momentum + Tendencia
  - Dirección: Principalmente largo
  - Indicadores: RSI, ATR, Media Móvil
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Corto/medio
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
