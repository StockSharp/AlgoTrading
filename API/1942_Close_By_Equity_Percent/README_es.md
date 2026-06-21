# Estrategia de Cierre por Porcentaje de Capital
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de gestión de riesgo monitorea el capital de la cartera y cierra cualquier posición abierta cuando el capital supera el balance actual multiplicado por un multiplicador definido por el usuario. Está diseñada para asegurar ganancias una vez que el valor de la cuenta alcanza un porcentaje deseado sobre el valor base.

La estrategia realiza verificaciones periódicas utilizando velas y no genera entradas de operaciones por sí misma; solo gestiona una posición existente. Tras el cierre, el balance de referencia se actualiza, permitiendo que el proceso se repita para operaciones posteriores.

## Detalles

- **Criterios de entrada**: Ninguno (gestiona la posición existente).
- **Largo/Corto**: Ambos direcciones.
- **Criterios de salida**: Capital mayor que `balance * EquityPercentFromBalance`.
- **Stops**: No.
- **Valores predeterminados**:
  - `EquityPercentFromBalance` = 1.2m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Gestión de riesgos
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía (1m)
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo

