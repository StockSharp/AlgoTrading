# Estrategia Buscador de Reversiones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El Buscador de Reversiones busca velas de gran rango que crean nuevos extremos y cierran de vuelta hacia el lado opuesto de la barra.
Compra cuando el precio lleva el precio a un nuevo mínimo pero termina cerca del máximo, y vende cuando el precio sube a un nuevo máximo pero cierra cerca del mínimo.

## Detalles

- **Criterios de entrada**: expansión de rango con cierre cerca del extremo opuesto después de un nuevo máximo/mínimo
- **Largo/Corto**: Ambos
- **Criterios de salida**: señal opuesta
- **Stops**: No
- **Valores predeterminados**:
  - `Lookback` = 20
  - `SmaLength` = 20
  - `RangeMultiple` = 1.5
  - `RangeThreshold` = 0.5
- **Filtros**:
  - Categoría: Patrón
  - Dirección: Ambos
  - Indicadores: SMA, Highest, Lowest
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

