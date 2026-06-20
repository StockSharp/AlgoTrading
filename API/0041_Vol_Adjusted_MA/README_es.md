# Volatility Adjusted Moving Average
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta técnica modifica una banda de media móvil por un múltiplo del ATR. Cuando el precio se mueve más allá de la banda ajustada, indica una tendencia acelerada.

Las pruebas indican un rendimiento anual promedio de aproximadamente 160%. Funciona mejor en el mercado forex.

Las operaciones largas se abren por encima de la banda superior, las cortas por debajo de la banda inferior. Un cruce de vuelta a través de la media móvil base cierra la posición.

Dado que las bandas se expanden con la volatilidad, los stops se adaptan a las condiciones del mercado.

## Detalles

- **Criterios de entrada**: El precio rompe por encima o por debajo de MA ± multiplicador ATR.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio cruza la MA o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `ATRPeriod` = 14
  - `ATRMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: MA, ATR
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

