# Estrategia Keltner Rsi
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que combina los indicadores Keltner Channels y RSI. Busca oportunidades de reversión a la media cuando el precio toca los límites del canal y el RSI confirma condiciones de sobreventa/sobrecompra.

Las pruebas indican un rendimiento anual promedio de aproximadamente 88%. Funciona mejor en el mercado de acciones.

Los Keltner Channels mapean la volatilidad reciente mientras el RSI mide los extremos del impulso. Las entradas ocurren cuando el RSI respalda un movimiento más allá del canal.

Ideal para traders de rebote en torno a envolventes de volatilidad. Los stops dependen de un multiplicador de ATR.

## Detalles

- **Criterios de entrada**:
  - Largo: `Close < LowerBand && RSI < RsiOversoldLevel`
  - Corto: `Close > UpperBand && RSI > RsiOverboughtLevel`
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - El precio regresa a la EMA
- **Stops**: Basados en porcentaje usando `StopLossPercent`
- **Valores predeterminados**:
  - `EmaPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2.0m
  - `RsiPeriod` = 14
  - `RsiOverboughtLevel` = 70m
  - `RsiOversoldLevel` = 30m
  - `StopLossPercent` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Reversión a la media
  - Dirección: Ambos
  - Indicadores: Keltner Channel, RSI
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Medio plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio

