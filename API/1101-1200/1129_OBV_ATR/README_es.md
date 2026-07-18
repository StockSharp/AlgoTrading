# Estrategia OBV ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia rastrea el On-Balance Volume (OBV) y entra en operaciones cuando el OBV rompe su máximo o mínimo reciente. Mantiene un canal dinámico similar a una Ruptura de ATR, alternando entre modos alcista y bajista.

## Detalles

- **Criterios de entrada**: OBV cruza por encima del máximo anterior para largo; cruza por debajo del mínimo anterior para corto.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta u órdenes de protección.
- **Stops**: Sí.
- **Valores predeterminados**:
  - `LookbackLength` = 30
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: OBV, Highest, Lowest
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
