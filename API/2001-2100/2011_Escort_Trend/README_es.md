# Estrategia de Tendencia Escort
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La Estrategia de Tendencia Escort combina una Media Móvil Ponderada (WMA) rápida y lenta con confirmación de MACD y CCI. Se abre una posición larga cuando la WMA rápida está por encima de la WMA lenta, la línea principal del MACD cruza por encima de la línea de señal y el CCI supera un umbral positivo. Una posición corta se activa en las condiciones opuestas. La estrategia usa opcionalmente stop loss fijo, take profit y trailing stop.

## Detalles
- **Criterios de entrada**:
  - **Largo**: `FastWMA > SlowWMA` Y `MACD > Signal` Y `CCI > +Threshold`.
  - **Corto**: `FastWMA < SlowWMA` Y `MACD < Signal` Y `CCI < -Threshold`.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal de entrada opuesta.
  - Stop loss, take profit o trailing stop opcionales.
- **Stops**: Sí, definidos por el usuario.
- **Valores predeterminados**:
  - `Fast WMA` = 8
  - `Slow WMA` = 18
  - `CCI Period` = 14
  - `CCI Threshold` = 100
  - `MACD Fast EMA` = 8
  - `MACD Slow EMA` = 18
  - `Take Profit` = 200
  - `Stop Loss` = 55
  - `Trailing Stop` = 35
  - `Trailing Step` = 3
- **Filtros**:
  - Categoría: Seguimiento de tendencia
  - Dirección: Ambos
  - Indicadores: Múltiples
  - Stops: Sí
  - Complejidad: Moderado
  - Marco temporal: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
