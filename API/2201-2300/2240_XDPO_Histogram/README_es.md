# Estrategia de Histograma XDPO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia de Histograma XDPO adapta el experto MQL5 original *Exp_XDPO_Histogram*. Construye un oscilador de precio destendenciado con doble suavizado (XDPO) a partir de precios de cierre. El oscilador se obtiene restando una media móvil al precio y suavizando esta diferencia con una segunda media móvil. La dinámica del histograma proporciona señales para abrir y cerrar operaciones.

## Lógica de trading

- Cuando el oscilador gira hacia arriba, se cierran todas las posiciones cortas. Si el valor actual del oscilador supera el anterior, se abre una nueva posición larga.
- Cuando el oscilador gira hacia abajo, se cierran todas las posiciones largas. Si el valor actual del oscilador está por debajo del anterior, se abre una nueva posición corta.
- Los cálculos se realizan solo en velas completadas.

## Parámetros

- `FirstMaLength` – longitud de la primera media móvil aplicada al precio.
- `SecondMaLength` – longitud de la media móvil aplicada a la diferencia entre el precio y la primera MA.
- `CandleType` – tipo de vela utilizado para todos los cálculos.

## Notas

- Las medias móviles se implementan con indicadores `SimpleMovingAverage`.
- La estrategia utiliza órdenes de mercado (`BuyMarket` y `SellMarket`) y cierra las posiciones opuestas antes de abrir nuevas.
