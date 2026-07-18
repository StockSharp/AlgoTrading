# Estrategia Puria
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Puria es una estrategia de seguimiento de tendencia que combina una EMA rápida, dos LWMA lentas del precio mínimo y un filtro MACD. Se abre una posición larga cuando la EMA de 5 períodos está por encima de ambas LWMA de 75 y 85 períodos, el cierre anterior está por encima de la EMA y la línea MACD es positiva. Se abre una posición corta cuando se cumplen las condiciones opuestas. La estrategia usa niveles fijos de take-profit y stop-loss y permite solo una posición por dirección hasta que aparezca una señal opuesta.

## Detalles
- **Criterios de entrada**: EMA(5) por encima de LWMA(75) y LWMA(85), cierre anterior por encima de EMA, MACD(15,26) > 0 para largos; inverso para cortos.
- **Largo/Corto**: Ambos.
- **Criterios de salida**: Stop-loss o take-profit.
- **Stops**: Distancias fijas de stop-loss y take-profit en puntos de precio.
- **Valores predeterminados**:
  - `StopLoss` = 14
  - `TakeProfit` = 15
  - `Ma1Period` = 75
  - `Ma2Period` = 85
  - `Ma3Period` = 5
  - `CandleType` = Marco temporal de 1 minuto
- **Filtros**: Filtro de línea cero MACD.
