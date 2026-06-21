# Estrategia Lorenzo SuperScalp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia de scalping combina RSI, Bandas de Bollinger y MACD. Compra cuando el RSI está por debajo de 45, el precio está cerca de la banda inferior y el MACD cruza hacia arriba. Vende cuando el RSI está por encima de 55, el precio está cerca de la banda superior y el MACD cruza hacia abajo. Un número mínimo de barras entre operaciones evita la re-entrada rápida.

## Detalles

- **Criterios de entrada**:
  - **Largo**: `RSI < 45` && `Close < LowerBand * 1.02` && `MACD` cruza por encima de la señal.
  - **Corto**: `RSI > 55` && `Close > UpperBand * 0.98` && `MACD` cruza por debajo de la señal.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Señal opuesta.
- **Stops**: Ninguno.
- **Valores predeterminados**:
  - `RSI Length` = 14
  - `Bollinger Length` = 20
  - `Bollinger Multiplier` = 2
  - `MACD Fast` = 12
  - `MACD Slow` = 26
  - `MACD Signal` = 9
  - `Min Bars` = 15
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: No
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
