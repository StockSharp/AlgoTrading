# Estrategia Super Woodies CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una conversión del asesor experto original MQL5 *Exp_SuperWoodiesCCI*. Opera basándose en la dirección del Índice de Canal de Materias Primas (CCI) calculado en un marco temporal superior.

## Lógica

- Calcular el CCI con un período configurable.
- Cuando el CCI cruza por encima de cero:
  - Opcionalmente cerrar posiciones cortas.
  - Opcionalmente abrir una posición larga.
- Cuando el CCI cruza por debajo de cero:
  - Opcionalmente cerrar posiciones largas.
  - Opcionalmente abrir una posición corta.

Solo se procesan velas completadas y la estrategia opera en un tipo de vela especificado.

## Parámetros

- **CciPeriod** – período para el cálculo del CCI.
- **CandleType** – marco temporal de velas a analizar.
- **AllowLongEntry** – habilitar apertura de posiciones largas.
- **AllowShortEntry** – habilitar apertura de posiciones cortas.
- **AllowLongExit** – habilitar cierre de posiciones largas cuando el CCI es negativo.
- **AllowShortExit** – habilitar cierre de posiciones cortas cuando el CCI es positivo.

## Notas

La estrategia utiliza la API de alto nivel de StockSharp con `SubscribeCandles` y enlace de indicadores. Los métodos de trading `BuyMarket` y `SellMarket` se usan para la gestión de posiciones.
