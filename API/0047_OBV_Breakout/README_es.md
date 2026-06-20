# OBV Breakout
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)
 
On-Balance Volume (OBV) rastrea la presión compradora y vendedora acumulando volumen. Esta estrategia busca que el OBV rompa por encima de un máximo o por debajo de un mínimo en la ventana de observación mientras el precio confirma el movimiento.

Las pruebas indican un retorno anual promedio de aproximadamente 178%. Funciona mejor en el mercado de acciones.

Un rompimiento en OBV sugiere un fuerte interés. El sistema entra largo si OBV supera su máximo anterior, o corto si rompe el mínimo. Cruzar la media móvil del OBV señala una salida.

Esto combina el momentum de volumen con la acción del precio.

## Detalles

- **Criterios de entrada**: OBV supera el valor más alto o más bajo en el período de observación.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: OBV cruza su MA o stop.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `LookbackPeriod` = 20
  - `OBVMAPeriod` = 20
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: OBV, MA
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

