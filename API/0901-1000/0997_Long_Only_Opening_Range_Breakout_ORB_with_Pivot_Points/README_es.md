# Solo Largos: Ruptura del Rango de Apertura (ORB) con Puntos Pivote
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia compra cuando el precio rompe por encima del máximo del rango de apertura y la primera resistencia (R1) de los pivotes diarios está por encima de ese máximo. Un stop trailing sigue los niveles de pivote.

## Detalles

- **Criterios de entrada**:
  - Tras el rango de apertura, entrar largo en una ruptura por encima del máximo de sesión si R1 es más alto.
- **Largo/Corto**: Solo largos
- **Criterios de salida**:
  - Stop trailing ajustado a los niveles de pivote y cierre diario.
- **Stops**: Sí
- **Valores predeterminados**:
  - `RangeMinutes` = 15
  - `SessionStart` = 09:30
  - `MaxTradesPerDay` = 1
  - `StopLossPercent` = 3
  - `InitialSlType` = Percentage
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo
  - Indicadores: Pivot Points
  - Stops: Sí
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
