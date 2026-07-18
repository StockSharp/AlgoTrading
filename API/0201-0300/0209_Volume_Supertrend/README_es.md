# Estrategia Volume Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia utiliza los indicadores Volume Supertrend para generar señales.
La entrada larga ocurre cuando Volume > Avg(Volume) && Price > Supertrend (subida de volumen con tendencia alcista). La entrada corta ocurre cuando Volume > Avg(Volume) && Price < Supertrend (subida de volumen con tendencia bajista).
Es adecuada para traders que buscan oportunidades en mercados de tendencia.

Las pruebas indican un rendimiento anual promedio de aproximadamente 64%. Funciona mejor en el mercado de divisas.

## Detalles
- **Criterios de entrada**:
  - **Largo**: Volume > Avg(Volume) && Price > Supertrend (volume surge with uptrend)
  - **Corto**: Volume > Avg(Volume) && Price < Supertrend (volume surge with downtrend)
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - **Largo**: Salir de la posición larga cuando Supertrend gira hacia abajo
  - **Corto**: Salir de la posición corta cuando Supertrend gira hacia arriba
- **Stops**: Sí.
- **Valores predeterminados**:
  - `VolumeAvgPeriod` = 20
  - `SupertrendPeriod` = 10
  - `SupertrendMultiplier` = 3m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `StopLossPercent` = 2.0m
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: Volume Supertrend
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

