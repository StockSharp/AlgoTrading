# Filtro de Reducción de Retraso de Series Temporales por Cryptorhythms
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia basada en el filtro de reducción de retraso de EMA.

El algoritmo compara el precio con una EMA ajustada al retraso y opera en las cruces.

## Detalles

- **Criterios de entrada**: Precio cruzando la EMA con retraso reducido.
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Cruce opuesto.
- **Stops**: No.
- **Valores predeterminados**:
  - `LagReduction` = 20m
  - `EmaLength` = 100
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Ambos
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (5m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
