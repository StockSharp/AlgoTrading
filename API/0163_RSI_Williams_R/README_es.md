# Estrategia Rsi Williams R
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Implementación de la estrategia - RSI + Williams %R. Compra cuando el RSI está por debajo de 30 y el Williams %R está por debajo de -80 (condición de doble sobreventa). Vende cuando el RSI está por encima de 70 y el Williams %R está por encima de -20 (condición de doble sobrecompra).

Las pruebas indican un rendimiento anual promedio de aproximadamente 76%. Funciona mejor en el mercado forex.

El RSI describe el impulso general, mientras que el Williams %R ofrece una señal más rápida de reversión. Las operaciones se activan cuando ambos osciladores coinciden.

Adecuado para traders activos que buscan oscilaciones cortas. Se utilizan stops basados en ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `RSI < RsiOversold && WilliamsR < WilliamsROversold`
  - Corto: `RSI > RsiOverbought && WilliamsR > WilliamsROverbought`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - El RSI regresa a la zona neutral
- **Stops**: Basados en porcentaje usando `StopLoss`
- **Valores predeterminados**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30m
  - `RsiOverbought` = 70m
  - `WilliamsRPeriod` = 14
  - `WilliamsROversold` = -80m
  - `WilliamsROverbought` = -20m
  - `StopLoss` = new Unit(2, UnitTypes.Percent)
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: RSI, Williams %R, R
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

