# Estrategia de Relleno de Brechas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Relleno de Brechas busca brechas de precio nocturnas al inicio de una nueva sesión. Cuando aparece una brecha, la estrategia opera en su contra esperando un movimiento de regreso al precio del día anterior o, si está invertida, opera en la dirección de la brecha con un stop en el nivel de la brecha.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**:
  - **Largo**: Nueva sesión y brecha a la baja (o brecha al alza si está invertido).
  - **Corto**: Nueva sesión y brecha al alza (o brecha a la baja si está invertido).
- **Criterios de salida**:
  - Precio de relleno de brecha alcanzado (objetivo de ganancia) o, cuando está invertido, el precio toca el stop en el nivel de la brecha.
- **Stops**: Utiliza el precio de la sesión anterior como objetivo/stop.
- **Valores predeterminados**:
  - `CandleType` = 1 minute
  - `Invert` = false
  - `CloseWhen` = NewSession
- **Filtros**:
  - Categoría: Trading de brechas
  - Dirección: Largo y Corto
  - Indicadores: Ninguno
  - Complejidad: Simple
  - Nivel de riesgo: Medio
