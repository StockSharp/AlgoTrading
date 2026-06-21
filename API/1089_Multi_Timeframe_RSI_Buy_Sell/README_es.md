# Estrategia de Compra/Venta RSI Multi-Temporalidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia utiliza valores RSI de tres marcos temporales diferentes. Se abre una posición larga cuando todos los RSI habilitados están por debajo del umbral de compra. Se abre una posición corta cuando todos los RSI habilitados están por encima del umbral de venta. Un período de enfriamiento evita señales consecutivas.

## Detalles

- **Criterios de entrada**: Todos los RSI activos por debajo/encima de los umbrales.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: No.
- **Valores predeterminados**:
  - `Rsi1Length` = 14
  - `Rsi2Length` = 14
  - `Rsi3Length` = 14
  - `Rsi1CandleType` = TimeSpan.FromMinutes(5)
  - `Rsi2CandleType` = TimeSpan.FromMinutes(15)
  - `Rsi3CandleType` = TimeSpan.FromMinutes(30)
  - `BuyThreshold` = 30m
  - `SellThreshold` = 70m
  - `CooldownPeriod` = 5
- **Filtros**:
  - Categoría: Momentum
  - Dirección: Ambos
  - Indicadores: RSI
  - Stops: No
  - Complejidad: Básico
  - Marco temporal: Multi-temporalidad
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
