# Estrategia Vicious Mortgage Rates V1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera un índice sintético construido a partir de cuatro medidas de volatilidad.
Se abre una posición larga cuando la EMA rápida del producto cruza por encima de la EMA lenta, y una posición corta en el cruce opuesto.

## Detalles

- **Criterios de entrada**: EMA rápida del índice combinado cruza por encima de la EMA lenta
- **Largo/Corto**: Ambos
- **Criterios de salida**: cruce opuesto
- **Stops**: No
- **Valores predeterminados**:
  - `FastLength` = 8
  - `SlowLength` = 21
- **Filtros**:
  - Categoría: Volatilidad
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Diario
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
