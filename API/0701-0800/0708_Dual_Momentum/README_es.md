# Estrategia de Momentum Dual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Rota entre un activo de riesgo y un activo seguro utilizando momentum dual.
La estrategia invierte en el activo de riesgo solo cuando su momentum es positivo y mayor que el momentum del activo seguro.

## Detalles

- **Criterios de entrada**: Momentum de riesgo > 0 y > momentum seguro
- **Largo/Corto**: Solo largos
- **Criterios de salida**: Cambiar al activo seguro cuando la condición falla
- **Stops**: No
- **Valores predeterminados**:
  - `Period` = 12
  - `CandleType` = TimeSpan.FromDays(30).TimeFrame()
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Solo largos
  - Indicadores: RateOfChange
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Mensual
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
