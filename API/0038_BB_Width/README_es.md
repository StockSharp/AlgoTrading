# Bollinger Band Width Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
La anchura de las Bollinger Bands mide la separación entre las bandas superior e inferior. Una anchura en expansión sugiere volatilidad y posible formación de tendencia. Esta estrategia opera rompimientos cuando la anchura está aumentando.

Las pruebas indican un rendimiento anual promedio de aproximadamente 151%. Funciona mejor en el mercado de acciones.

La posición del precio relativa a la banda media establece la dirección. Un canal ensanchándose con el precio por encima de la banda media activa compras, mientras que un canal ensanchándose por debajo activa ventas.

Las salidas ocurren cuando la anchura de banda se contrae o se alcanza un stop de volatilidad.

## Detalles

- **Criterios de entrada**: Anchura de banda en expansión y precio relativo a la banda media.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Anchura de banda se contrae o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, ATR
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

