# Estrategia Big Runner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Big Runner opera cuando el precio de cierre y una SMA rápida cruzan en la dirección de una SMA más lenta, indicando un fuerte momentum. El tamaño de la posición se deriva de un porcentaje del valor del portafolio multiplicado por el apalancamiento. Niveles opcionales de stop-loss y take-profit gestionan el riesgo.

## Detalles

- **Criterios de entrada**:
  - Comprar cuando el cierre cruza hacia arriba la SMA rápida y la SMA rápida cruza hacia arriba la SMA lenta.
  - Vender cuando el cierre cruza hacia abajo la SMA rápida y la SMA rápida cruza hacia abajo la SMA lenta.
- **Largo/Corto**: Largo y corto.
- **Criterios de salida**:
  - Stop-loss y take-profit opcionales basados en el precio de entrada.
  - La señal contraria cierra la posición existente.
- **Stops**: Porcentajes de stop-loss y take-profit configurables.
- **Valores predeterminados**:
  - `FastLength` = 5
  - `SlowLength` = 20
  - `TakeProfitLongPercent` = 4
  - `TakeProfitShortPercent` = 7
  - `StopLossLongPercent` = 2
  - `StopLossShortPercent` = 2
  - `PercentOfPortfolio` = 10
  - `Leverage` = 1
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Largo y Corto
  - Indicadores: SMA
  - Stops: Sí
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
