# ATR Trailing Stops
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
ATR Trailing usa un múltiplo del average true range para arrastrar stops detrás de las posiciones abiertas. Las entradas ocurren cuando el precio cruza una media móvil, y el stop de seguimiento se ajusta con la volatilidad.

Las pruebas indican un rendimiento anual promedio de aproximadamente 157%. Funciona mejor en el mercado de criptomonedas.

A medida que el precio avanza, el stop se desplaza hacia arriba (o hacia abajo) basándose en la última lectura de ATR, sin retroceder nunca. Esto bloquea las ganancias mientras persiste la tendencia.

Las salidas ocurren cuando se activa el stop de seguimiento o cuando el precio cruza de vuelta a través de la media móvil.

## Detalles

- **Criterios de entrada**: Precio por encima o por debajo de la MA.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Stop de seguimiento activado o precio cruza la MA.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 3.0m
  - `MAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: ATR, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

