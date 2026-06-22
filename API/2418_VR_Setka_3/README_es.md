# Estrategia VR Setka 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia VR Setka 3** implementa un enfoque de trading en rejilla. La estrategia coloca órdenes límite de compra y venta simétricas alrededor del precio actual del mercado. Después de que se ejecuta una orden, el nivel de take-profit se recalcula usando el precio promedio de entrada de todas las posiciones en la dirección activa. Las nuevas órdenes de rejilla se colocan con espaciado creciente y, opcionalmente, con volumen creciente (martingala).

## Parámetros
- **Start Offset** – distancia inicial desde el precio actual para el primer par de órdenes límite.
- **Take Profit** – distancia desde el precio promedio de entrada donde todas las posiciones se cierran con ganancia.
- **Grid Distance** – paso base entre los niveles de la rejilla.
- **Step Distance** – distancia adicional agregada para cada nivel de rejilla subsiguiente.
- **Use Martingale** – cuando está habilitado, cada nueva orden de rejilla aumenta su volumen usando el multiplicador.
- **Martingale Multiplier** – factor para el aumento de volumen cuando la martingala está activa.
- **Volume** – volumen base de la orden para el primer nivel.
- **Candle Type** – marco temporal usado para sincronizar las operaciones de la estrategia.

## Algoritmo
1. Al inicio, la estrategia coloca un **buy limit** por debajo y un **sell limit** por encima del precio actual.
2. Cuando un lado se ejecuta, la orden opuesta se cancela.
3. La estrategia recalcula un nivel de take-profit común en el precio promedio ± *Take Profit*.
4. Si el precio se mueve en contra de la posición, se coloca una nueva orden límite a **Grid Distance + Step Distance × nivel** desde el precio promedio. El volumen aumenta si la martingala está habilitada.
5. Cuando el precio alcanza el nivel de take-profit, todas las posiciones en esa dirección se cierran y la rejilla se reinicia.

## Notas
- La estrategia no abre posiciones en ambas direcciones simultáneamente.
- Se requiere una gestión adecuada del riesgo porque la martingala puede aumentar rápidamente el tamaño de la posición.
- Funciona con cualquier instrumento compatible con StockSharp siempre que el tipo de vela elegido esté disponible.
