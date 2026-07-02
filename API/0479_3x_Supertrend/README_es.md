# Estrategia 3x Supertrend
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia **3x Supertrend** utiliza tres bandas basadas en ATR con diferentes períodos y multiplicadores.
Se abre una posición larga cuando el precio sube por encima de las tres bandas y la banda rápida cambia a
tendencia alcista. La operación se cierra cuando el precio cae por debajo de todas las bandas, señalando la pérdida de impulso alcista.

## Detalles
- **Datos**: Velas de precio.
- **Criterios de entrada**: Precio por encima de todas las bandas y banda rápida virando al alza.
- **Criterios de salida**: Precio por debajo de todas las bandas.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `AtrPeriod1` = 11
  - `Factor1` = 1
  - `AtrPeriod2` = 12
  - `Factor2` = 2
  - `AtrPeriod3` = 13
  - `Factor3` = 3
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: Supertrend basado en ATR
  - Complejidad: Moderado
  - Nivel de riesgo: Medio
