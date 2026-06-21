# Estrategia Molly ETF EMA Crossover
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra en una posición larga cuando la EMA rápida cruza por encima de la EMA lenta y sale cuando la EMA rápida cruza por debajo. Incluye parámetros opcionales para restringir el trading a un rango de fechas específico.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La EMA rápida cruza por encima de la EMA lenta dentro del rango de fechas.
- **Largo/Corto**: Solo largos.
- **Criterios de salida**:
  - La EMA rápida cruza por debajo de la EMA lenta o el rango de fechas finaliza.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `Fast EMA` = 10
  - `Slow EMA` = 21
  - `Start Date` = 2018-01-01
  - `End Date` = 2023-09-07
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: EMA
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Bajo
