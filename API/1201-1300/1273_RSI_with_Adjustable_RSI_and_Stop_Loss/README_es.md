# Estrategia RSI con RSI Ajustable y Stop-Loss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Compra cuando el valor del RSI cae por debajo de un umbral y cierra la posición larga cuando el precio rompe por encima del máximo de la vela anterior. Un stop loss porcentual protege cada operación.

## Detalles

- **Criterios de entrada**:
  - Largo: RSI por debajo de `RsiThreshold`
- **Largo/Corto**: Largo
- **Criterios de salida**:
  - Precio de cierre por encima del máximo de la vela anterior
  - Stop loss
- **Stops**: Sí
- **Valores predeterminados**:
  - `RsiLength` = 8
  - `RsiThreshold` = 28m
  - `StopLossPercent` = 5m
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoría: Oscilador
  - Dirección: Largo
  - Indicadores: RSI
  - Stops: Sí
  - Complejidad: Básico
  - Marco temporal: Corto plazo
  - Estacionalidad: Ninguno
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
