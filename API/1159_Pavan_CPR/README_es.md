# Estrategia Pavan CPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera en largo cuando el precio cruza por encima del Rango de Pivote Central superior del día tras haber cerrado previamente por debajo de él. El stop se coloca en el nivel del pivote y el take profit a una distancia fija.

## Detalles

- **Criterios de entrada**: Cierre anterior por debajo del CPR superior y cierre actual por encima de él.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Take profit o stop en el pivote.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `TakeProfitTarget` = 50
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Largo
  - Indicadores: Pivot
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
