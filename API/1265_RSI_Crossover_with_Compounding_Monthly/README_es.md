# Estrategia de Cruce RSI con Capitalización Compuesta (Mensual)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia invierte todo el capital cuando el RSI mensual cierra por encima de su SMA y sale cuando el RSI cae por debajo de la SMA. Las ganancias se añaden al capital para capitalización compuesta.

Los backtests sugieren un retorno anual promedio de alrededor del 20%. Funciona mejor en acciones.

## Detalles

- **Criterios de entrada**: RSI por encima de su SMA
- **Largo/Corto**: Largo
- **Criterios de salida**: RSI por debajo de su SMA
- **Stops**: No
- **Valores predeterminados**:
  - `CandleType` = 1 mes
  - `RsiPeriod` = 14
  - `InitialCapital` = 100000
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Solo largos
  - Indicadores: RSI, SMA
  - Stops: No
  - Complejidad: Principiante
  - Marco temporal: Mensual
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
