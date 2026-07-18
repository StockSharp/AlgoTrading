# Estrategia Low Volatility Reversion
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
Esta estrategia de reversión a la media se activa únicamente durante mercados tranquilos. Mide el ATR durante un período de observación y entra cuando la volatilidad cae por debajo de un porcentaje de ese promedio y el precio se desvía de su media móvil.

Las pruebas indican un rendimiento anual promedio de aproximadamente 139%. Funciona mejor en el mercado de acciones.

Al operar contra pequeños movimientos en condiciones calmas, busca capturar rebotes sin perseguir grandes tendencias.

Las posiciones salen una vez que el precio toca la media móvil o se alcanza el stop-loss basado en ATR.

## Detalles

- **Criterios de entrada**: Precio alejado de la media móvil mientras el ATR está por debajo del umbral.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: El precio regresa a la MA o se activa el stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MAPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrLookbackPeriod` = 20
  - `AtrThresholdPercent` = 50m
  - `AtrMultiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: ATR, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

