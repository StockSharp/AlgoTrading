# Estrategia DEMA RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera los cruces entre el RSI de una media móvil exponencial doble y su valor suavizado. Se abre una posición larga cuando el RSI cruza al alza la línea suavizada y una posición corta en el cruce opuesto. Las posiciones pueden protegerse con take profit, stop trailing y un filtro de sesión de trading opcional.

## Parámetros
- Tipo de vela
- Longitud de MA
- Longitud de RSI
- Longitud de suavizado de RSI
- Puntos de take profit
- Puntos de stop trailing
- Usar sesión
- Inicio de sesión
- Fin de sesión

### Condiciones reales RSI(DEMA)

Predeterminados: `CandleType=1m`, `MaLength=20`, `RsiLength=14`, `RsiSmoothingLength=5`, `TakeProfitPoints=0`, `TrailStopPoints=0`, `UseSession=false`, `SessionStart=09:00`, `SessionEnd=17:00`. En vela terminada: `EMA1=EMA(Close,20)`, `EMA2=EMA(EMA1,20)`, `DEMA=2×EMA1−EMA2`, `R=RSI(DEMA,14)`, `S=SMA(R,5)`. Comprar si antes `R <= S` y ahora `R > S`; vender si antes `R >= S` y ahora `R < S`, desde plano o invirtiendo la posición opuesta con `Volume + abs(Position)`. `UseSession=true` limita nuevas entradas a `[SessionStart,SessionEnd)`, incluso cruzando medianoche; las salidas por protección son independientes. Los puntos de `TakeProfitPoints` y `TrailStopPoints` son **ticks mínimos del instrumento**: valores no nulos usan protección nativa a distancia absoluta, cero desactiva cada componente. El README no fijaba números predeterminados, definidos aquí. El antiguo cruce EMA120/450 no implementaba el indicador descrito.

