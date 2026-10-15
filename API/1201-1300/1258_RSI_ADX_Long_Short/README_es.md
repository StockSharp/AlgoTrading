# Estrategia RSI & ADX Largo/Corto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera en ambas direcciones utilizando RSI para las señales y ADX para la confirmación de tendencia.
Se abre una posición larga cuando el RSI cruza por encima de 70 y el ADX está por encima del umbral.
Se abre una posición corta cuando el RSI cruza por debajo de 30 y el ADX está por encima del umbral.
Las posiciones se cierran en cruces opuestos del RSI.

## Detalles

- **Criterios de entrada**: RSI cruza por encima de 70 para largos o por debajo de 30 para cortos con ADX por encima del umbral
- **Largo/Corto**: Ambos
- **Criterios de salida**: Cruces opuestos del RSI
- **Stops**: No
- **Valores predeterminados**:
  - `RsiLength` = 8
  - `AdxLength` = 20
  - `AdxThreshold` = 14
- **Filtros**:
  - Categoría: Indicador
  - Dirección: Ambos
  - Indicadores: RSI, ADX
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
