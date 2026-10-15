# Estrategia Brake Exp
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en el indicador **BrakeExp**. El indicador traza un canal adaptativo de soporte y resistencia construido a partir de una curva exponencial. Un cambio del canal de la línea inferior a la superior genera una señal de venta, y un cambio de la superior a la inferior genera una señal de compra.

## Cómo funciona

- Cuando el indicador reporta una **señal alcista**, la estrategia cierra posiciones cortas y abre una nueva posición larga.
- Cuando aparece una **señal bajista**, las posiciones largas existentes se cierran y se abre una posición corta.
- Si solo se detecta una **tendencia alcista**, la estrategia cierra posiciones cortas.
- Si solo se detecta una **tendencia bajista**, la estrategia cierra posiciones largas.

Las señales se procesan solo en velas cerradas.

## Parámetros

- `A` – factor de aceleración de la curva del indicador BrakeExp.
- `B` – paso de precio utilizado para el ancho del canal.
- `CandleType` – serie de velas para el cálculo del indicador.
- `Volume` – volumen de la orden utilizado al entrar al mercado.

## Notas

La estrategia utiliza la API de alto nivel de StockSharp y puede ejecutarse en Designer, Shell o cualquier otro producto StockSharp.
