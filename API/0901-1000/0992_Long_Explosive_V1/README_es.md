# Estrategia Long Explosive V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Long Explosive V1 abre una posición larga cuando el precio de cierre sube un porcentaje definido respecto a la barra anterior. La posición se cierra cuando el precio cae el porcentaje configurado o antes de abrir una nueva operación larga.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `Close - PrevClose > Close * Price increase (%) / 100`.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: `Close - PrevClose < -Close * Price decrease (%) / 100` o antes de una nueva entrada larga.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Price increase (%)` = 1
  - `Price decrease (%)` = 1
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Largo
  - Indicadores: Precio
  - Stops: No
  - Complejidad: Bajo
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
