# Estrategia de Todas las Divergencias
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Todas las Divergencias busca divergencias alcistas y bajistas del RSI filtradas por la tendencia de una media móvil. Se abre una posición larga cuando el precio marca un mínimo más bajo mientras el RSI forma un mínimo más alto por encima de la media móvil. Se abre una posición corta cuando el precio marca un máximo más alto mientras el RSI forma un máximo más bajo por debajo de la media móvil. Una protección opcional de stop-loss y take-profit puede cerrar posiciones automáticamente, y un control de riesgo por media móvil sale tras varios cierres contra la tendencia.

## Detalles

- **Criterios de entrada**:
  - La posición del precio respecto a la media móvil define la tendencia.
  - **Largo**: el precio hace un mínimo más bajo, el RSI un mínimo más alto, el precio por encima de la MA.
  - **Corto**: el precio hace un máximo más alto, el RSI un máximo más bajo, el precio por debajo de la MA.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal opuesta o salida por riesgo de MA.
- **Stops**: Stop-loss y take-profit opcionales.
- **Valores predeterminados**:
  - `MaLength` = 50
  - `RsiLength` = 14
  - `MaRiskCandles` = 3
  - `UseProtection` = False
- **Filtros**:
  - Categoría: Divergencia
  - Dirección: Ambos
  - Indicadores: RSI, Moving Average
  - Stops: Opcional
  - Complejidad: Moderado
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: Sí
  - Nivel de riesgo: Medio
