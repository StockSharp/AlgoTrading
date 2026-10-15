# Estrategia Two X SPY TIPS
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia asigna capital en el activo negociado cuando tanto el S&P 500 como los precios TIPS están por encima de sus medias móviles de 200 períodos al inicio de un nuevo mes.

## Detalles

- **Criterios de entrada**: S&P 500 y TIPS por encima de su SMA en un nuevo mes.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**: Sin salidas.
- **Stops**: No.
- **Valores predeterminados**:
  - `SmaLength` = 200
  - `Leverage` = 2
  - `CandleType` = TimeSpan.FromDays(1)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: SMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: Sí
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
