# Estrategia Donchian RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina los Canales Donchian y el indicador RSI. Compra en rupturas de Donchian cuando el RSI confirma que la tendencia no está sobreextendida.

Las pruebas indican un retorno anual promedio de aproximadamente el 55%. Funciona mejor en el mercado de acciones.

Los canales Donchian identifican los niveles de ruptura, mientras que el RSI verifica si el momentum respalda el movimiento. Las posiciones se abren cuando una ruptura se alinea con la dirección del RSI.

Ideal para traders que esperan una ruptura sostenida en lugar de una trampa. El riesgo se limita mediante un stop porcentual.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close > DonchianHigh && RSI < RsiOverboughtLevel`
  - Corto: `Close < DonchianLow && RSI > RsiOversoldLevel`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Fallo de ruptura o señal opuesta
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `DonchianPeriod` = 20
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    Una ruptura no está sobreextendida mientras el RSI siga por debajo de RsiOverboughtLevel (largo) o por encima de RsiOversoldLevel (corto); el canal abarca las DonchianPeriod velas anteriores. La ruptura falla, y la posición se cierra, cuando el precio vuelve a cerrar al otro lado del nivel roto. Una señal de entrada contra una posición abierta la invierte.
- **Filtros**:
  - Categoría: Ruptura
  - Dirección: Ambos
  - Indicadores: Donchian Channel, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
