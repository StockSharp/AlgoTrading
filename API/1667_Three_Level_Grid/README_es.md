# Estrategia de Cuadrícula de Tres Niveles
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia implementa un sistema de trading en cuadrícula simétrica con hasta tres rangos de take-profit.
Las órdenes límite se colocan por encima y por debajo del precio actual a intervalos fijos. Cuando una
orden de entrada se ejecuta, se envía una orden límite opuesta para capturar ganancias a una distancia
configurable. El método es adecuado para mercados laterales donde el precio oscila dentro de una banda.

## Parámetros

- `Grid Size` – distancia entre niveles de la cuadrícula.
- `Levels` – número de niveles de cuadrícula en cada lado del precio actual.
- `Base Take Profit` – distancia de ganancia base para el primer rango.
- `Order Volume` – volumen utilizado para cada orden de cuadrícula.
- `Enable Rank1` – colocar órdenes con take-profit base.
- `Enable Rank2` – colocar órdenes con take-profit base más un tamaño de cuadrícula.
- `Enable Rank3` – colocar órdenes con take-profit base más dos tamaños de cuadrícula.
- `Allow Longs` – habilitar el lado largo de la cuadrícula.
- `Allow Shorts` – habilitar el lado corto de la cuadrícula.
- `Candle Type` – tipo de vela usado para obtener el precio de referencia inicial.

## Lógica de Trading

1. Al inicio, la estrategia se suscribe a velas y espera la primera vela completada.
2. Usando el precio de cierre de esa vela, se construye la cuadrícula con el número de niveles configurado.
3. Para cada nivel se colocan órdenes límite de compra y/o venta dependiendo de los lados permitidos.
4. Cuando una orden de entrada se ejecuta, se registra una orden límite opuesta al precio de take-profit
   calculado según el rango seleccionado.
5. Las órdenes permanecen en el mercado hasta que se ejecutan o se cancelan manualmente.

Esta implementación es una conversión simplificada del sistema de cuadrícula MQL original y tiene como
objetivo destacar la mecánica central en la API de alto nivel de StockSharp.
