# Estrategia Marneni Money Tree
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia traduce el asesor experto MQL "Marneni Money Tree" a StockSharp.
Se basa en una media móvil simple (SMA) de 40 períodos y dos valores desplazados para detectar la dirección de la tendencia.
Cuando la SMA desplazada cuatro barras se encuentra entre la SMA actual y el valor de treinta barras atrás,
- se envía una orden de mercado en la dirección detectada;
- se colocan ocho órdenes límite adicionales a distancias crecientes, definidas por `Order2Pips` hasta `Order9Pips`.

Las configuraciones largas colocan compras límite por debajo del precio actual. Las configuraciones cortas colocan ventas límite por encima del precio.
Las posiciones se cierran y las órdenes restantes se cancelan cuando la relación de la SMA se invierte.

## Parámetros
- `Order2Pips`–`Order9Pips` — distancia en pips para las órdenes límite 2 a 9.
- `CandleType` — marco temporal utilizado para los cálculos.

El volumen base de la operación está fijado en 2 y puede ajustarse cambiando la propiedad `Volume` antes de iniciar la estrategia.
