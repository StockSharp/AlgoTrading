# Estrategia Grid TLong V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en cuadrícula que mantiene continuamente una posición. Vuelve a entrar en posiciones cuando la ganancia o pérdida alcanza un paso porcentual fijo.

## Detalles

- **Criterios de entrada**: Siempre en el mercado; reabrir posiciones en los pasos de la cuadrícula.
- **Largo/Corto**: Ambas direcciones.
- **Criterios de salida**: Señal opuesta o reentrada tras alcanzar el paso porcentual.
- **Stops**: No.
- **Valores predeterminados**:
  - `Percent` = 1
  - `UseLimitOrders` = false
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Cuadrícula
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
