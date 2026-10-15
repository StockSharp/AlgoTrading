# Estrategia de Seguimiento de Tendencia MM3 Máximos y Mínimos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Utiliza una media móvil simple de 3 periodos de máximos y mínimos. Una operación larga se abre cuando el precio cierra por encima de la SMA de máximos y se cierra cuando el precio cae por debajo de la SMA de mínimos.

## Detalles

- **Criterios de entrada**: Close > SMA(high).
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Close < SMA(low).
- **Stops**: No.
- **Valores predeterminados**:
  - `Length` = 3
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
