# Estrategia TTM Squeeze
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia TTM Squeeze busca períodos de compresión de precios cuando las Bollinger
Bands se contraen dentro de los Keltner Channels. Este "squeeze" señala una posible
expansión de volatilidad. Durante el squeeze, la estrategia monitorea un oscilador de
momentum de regresión lineal y el RSI para medir la dirección. Cuando el squeeze se
libera y el momentum gira, se toman posiciones en la dirección del movimiento.

El método busca rupturas explosivas desde rangos tranquilos. Las operaciones se filtran
de modo que las configuraciones largas requieren que el momentum suba desde debajo de
cero con RSI por encima de 30, mientras que las cortas necesitan que el momentum caiga
desde territorio positivo con RSI por debajo de 70. Un parámetro opcional de take-profit
puede cerrar automáticamente las operaciones con una ganancia predefinida.

## Detalles

- **Criterios de entrada**:
  - Squeeze desactivado (Bollinger Bands fuera de Keltner Channels).
  - **Largo**: Momentum < 0 y subiendo, RSI > 30.
  - **Corto**: Momentum > 0 y bajando, RSI < 70.
- **Largo/Corto**: Ambos lados.
- **Criterios de salida**:
  - Señal opuesta o take-profit si está habilitado.
- **Stops**: Ninguno por defecto, take-profit opcional.
- **Valores predeterminados**:
  - `SqueezeLength` = 20
  - `RsiLength` = 14
  - `UseTP` = False
  - `TpPercent` = 1.2
- **Filtros**:
  - Categoría: Ruptura de volatilidad
  - Dirección: Ambos
  - Indicadores: Bollinger Bands, Keltner Channels, RSI, Regresión lineal
  - Stops: Opcional
  - Complejidad: Medio
  - Marco temporal: Cualquiera
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: Medio
