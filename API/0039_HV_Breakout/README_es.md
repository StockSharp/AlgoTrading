# Historical Volatility Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Este método de rompimiento usa la volatilidad histórica para establecer umbrales dinámicos. Cuando el precio se mueve más allá de un nivel de referencia por más de la volatilidad actual, indica una tendencia potencial.

Las pruebas indican un rendimiento anual promedio de aproximadamente 154%. Funciona mejor en el mercado de acciones.

La estrategia compara el precio con niveles derivados de la desviación estándar y una media móvil simple. Los rompimientos por encima o por debajo de esos niveles activan operaciones.

Las salidas ocurren cuando el precio cruza de vuelta a través de la media móvil o el stop se activa.

## Detalles

- **Criterios de entrada**: El precio rompe por encima o por debajo del nivel basado en HV.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio cruza la MA o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `HvPeriod` = 20
  - `MAPeriod` = 20
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: HV, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

