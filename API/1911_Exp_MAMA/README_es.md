# Estrategia Exp MAMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera utilizando el indicador MESA Adaptive Moving Average (MAMA).

El indicador produce dos líneas:

- **MAMA** – la media móvil adaptativa.
- **FAMA** – una media de seguimiento utilizada como línea de señal.

Lógica de operación:

1. Cuando MAMA cruza por debajo de FAMA, la estrategia cierra posiciones cortas y abre una nueva posición larga.
2. Cuando MAMA cruza por encima de FAMA, la estrategia cierra posiciones largas y abre una nueva posición corta.

## Parámetros

- `FastLimit` – límite alfa rápido utilizado por el factor adaptativo.
- `SlowLimit` – límite alfa lento utilizado por el factor adaptativo.
- `CandleType` – marco temporal para las velas entrantes.
- `BuyOpen` / `SellOpen` – permiten abrir posiciones largas o cortas.
- `BuyClose` / `SellClose` – permiten cerrar posiciones largas o cortas.

La estrategia opera en velas terminadas y utiliza órdenes de mercado para entrada y salida.
