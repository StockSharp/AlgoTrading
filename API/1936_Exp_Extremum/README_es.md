# Estrategia Exp Extremum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera reversiones detectadas comparando los extremos de precio en una ventana de retrospección. Observa si la vela actual lleva el precio más allá de los máximos o mínimos anteriores y reacciona cuando el signo de esta comparación cambia.

## Cómo funciona

1. Para cada vela completada la estrategia encuentra:
   - El máximo más bajo en los últimos *N* barones.
   - El mínimo más alto en los últimos *N* barones.
2. Se suman las diferencias entre el máximo/mínimo actual y estos niveles.
3. Una suma positiva indica presión alcista, una suma negativa indica presión bajista.
4. Cuando el signo de hace dos barones se opone al signo del barón anterior, aparece una señal de reversión:
   - Arriba luego Abajo → abrir posición larga.
   - Abajo luego Arriba → abrir posición corta.
5. Los permisos opcionales permiten deshabilitar independientemente la apertura o cierre de posiciones largas/cortas.

## Parámetros

- `Length` – período del indicador para cálculos de extremos.
- `CandleType` – marco temporal de las velas entrantes.
- `BuyPosOpen` / `SellPosOpen` – permisos para abrir posiciones largas o cortas.
- `BuyPosClose` / `SellPosClose` – permisos para cerrar posiciones largas o cortas.

## Notas

La estrategia utiliza la API de alto nivel con suscripciones de velas e indicadores integrados `Highest`/`Lowest`. Las posiciones se abren con órdenes de mercado y se cierran mediante `ClosePosition()` cuando aparece la señal opuesta.
