# Estrategia de Cruce de Cero CMO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en los cruces de la línea cero del Oscilador de Momento Chande (CMO).
Cuando el oscilador cruza por debajo de cero, se abre una posición larga. Cuando cruza por encima de
cero, se abre una posición corta. Niveles opcionales de stop loss y take profit (en puntos)
protegen la posición. Las entradas y salidas de operaciones largas y cortas pueden habilitarse
o deshabilitarse individualmente.

## Parámetros

- `Volume` – volumen de la orden.
- `CmoPeriod` – período para el indicador CMO.
- `StopLoss` – stop loss en puntos.
- `TakeProfit` – take profit en puntos.
- `AllowLongEntry` – permitir apertura de posiciones largas.
- `AllowShortEntry` – permitir apertura de posiciones cortas.
- `AllowLongExit` – permitir cierre de posiciones largas ante señal opuesta.
- `AllowShortExit` – permitir cierre de posiciones cortas ante señal opuesta.
- `CandleType` – marco temporal utilizado para los cálculos.

## Lógica de Trading

1. Suscribirse a velas del marco temporal seleccionado y calcular el CMO.
2. Cuando el CMO cruza de arriba hacia abajo de cero:
   - Cerrar posiciones cortas si está permitido.
   - Abrir una posición larga si está permitido.
3. Cuando el CMO cruza de abajo hacia arriba de cero:
   - Cerrar posiciones largas si está permitido.
   - Abrir una posición corta si está permitido.
4. Stop loss y take profit se aplican usando órdenes de protección en puntos.

## Notas

- Las decisiones de trading se toman únicamente en velas completadas.
- La estrategia utiliza la API de alto nivel de StockSharp y enlaza indicadores a través de `Bind`.
