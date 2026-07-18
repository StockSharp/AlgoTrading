# Estrategia RSI + MACD Solo Largos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia entra en largo cuando el RSI cruza por encima de la línea media con confirmación alcista del MACD, o cuando el MACD cruza por encima de su línea de señal mientras el RSI se mantiene por encima de la línea media. Las salidas ocurren cuando el RSI cae por debajo de la línea media o el MACD cruza por debajo de la señal con un histograma no positivo. Un filtro de tendencia EMA opcional y el contexto de sobrevendido pueden refinar las entradas.

## Detalles

- **Criterios de entrada**: RSI cruza por encima de la línea media con MACD alcista o MACD cruza por encima de la señal con RSI sobre la línea media
- **Largo/Corto**: Solo largos
- **Criterios de salida**: RSI cruza por debajo de la línea media o MACD cruza por debajo de la señal con histograma ≤ 0
- **Stops**: Take profit y stop loss en porcentaje opcionales
- **Valores predeterminados**:
  - `RsiLength` = 14
  - `RsiOversold` = 30
  - `RsiMidline` = 50
  - `FastLength` = 12
  - `SlowLength` = 26
  - `SignalLength` = 9
  - `OversoldWindowBars` = 10
  - `EmaLength` = 200
  - `TakeProfitPercent` = 11.5
  - `StopLossPercent` = 2.5
- **Filtros**:
  - Categoría: Tendencia
  - Dirección: Solo largos
  - Indicadores: RSI, MACD, EMA
  - Stops: Sí (opcional)
  - Complejidad: Intermedio
  - Marco temporal: Intradía
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
