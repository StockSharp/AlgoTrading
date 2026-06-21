# Estrategia de Compra en Caída con Múltiples Posiciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Buy Dip Multiple Positions añade posiciones largas cuando se produce una caída de precio junto con un alto volumen y una condición de impulso de precio. Cada operación arriesga el 2% del capital y comparte niveles comunes de stop dinámico y objetivo. Solo se abre una nueva posición si la operación anterior cerrada fue rentable.

## Detalles

- **Criterios de entrada**:
  - Cierre por debajo del mínimo anterior en un 0,2%.
  - Volumen superior al 120% de la media de las dos últimas barras.
  - Cierre por debajo del precio de cierre N barras atrás multiplicado por `PriceSurgePercent` / 100.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - Stop inicial como porcentaje del mínimo de la barra de entrada.
  - Stop dinámico que aumenta cada barra después del setup.
  - Precio objetivo por encima del mínimo de la barra de entrada.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `MaxPositions` = 20
  - `TrailRatePercent` = 1
  - `InitialStopPercent` = 85
  - `TargetPricePercent` = 60
  - `PriceSurgePercent` = 89
  - `SurgeLookbackBars` = 14
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Largo
  - Indicadores: Volumen, Acción del precio
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
