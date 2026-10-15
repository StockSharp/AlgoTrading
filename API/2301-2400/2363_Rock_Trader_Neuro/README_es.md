# Estrategia Rock Trader Neuro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia que opera utilizando Bandas de Bollinger y una neurona simple.
Los últimos siete anchos de las Bandas de Bollinger se normalizan al rango [-1,1] y
se combinan con pesos fijos. La suma ponderada pasa por una activación de tangente hiperbólica.
Una salida negativa abre una posición larga, mientras que una salida positiva abre una posición corta.
Las posiciones se cierran por stop loss o take profit.

## Detalles

- **Criterios de entrada**:
  - Largo: salida de la neurona < 0
  - Corto: salida de la neurona > 0
- **Largo/Corto**: Ambos
- **Criterios de salida**:
  - Stop loss o take profit alcanzado
- **Stops**: Distancia absoluta en precio
- **Valores predeterminados**:
  - `StopLoss` = 30
  - `TakeProfit` = 100
  - `Lot` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoría: Neural
  - Dirección: Ambos
  - Indicadores: Bollinger Bands
  - Stops: Sí
  - Complejidad: Intermedio
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: Sí
  - Divergencia: No
  - Nivel de riesgo: Medio
