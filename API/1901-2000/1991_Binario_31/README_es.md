# Estrategia Binario 31
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia de rompimiento convertida del script MetaTrader **binario_31**. El algoritmo construye dos medias móviles exponenciales de 144 períodos calculadas sobre los precios máximos y mínimos de las velas, creando un canal dinámico. Mientras el precio permanece dentro del canal, la estrategia prepara órdenes de stop de entrada:

- una compra stop colocada por encima de la EMA-alta más un desplazamiento configurable;
- una venta stop colocada por debajo de la EMA-baja menos el mismo desplazamiento.

Cuando el precio rompe uno de estos niveles, se abre una posición en la dirección del rompimiento. Se coloca un stop de protección en el lado opuesto del canal y se calcula un objetivo de take profit relativo a la entrada. Se puede activar un trailing stop opcional para proteger las ganancias.

## Parámetros

- **EMA Length** – período para ambas EMA de máximos y mínimos.
- **Pip Difference** – distancia desde el nivel de la EMA hasta la entrada de rompimiento en pasos de precio.
- **Take Profit** – distancia desde la entrada hasta el take profit en pasos de precio.
- **Trailing Stop** – distancia del trailing stop en pasos de precio. Establecer en cero para desactivar.
- **Volume** – volumen de la orden.
- **Candle Type** – tipo de velas a las que se suscribe la estrategia.
