# Estrategia Limits Martin
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia coloca órdenes límite pareadas por encima y por debajo del precio de mercado actual. Cada operación usa una distancia de paso configurable y un dimensionamiento de posición martingala opcional para recuperar pérdidas anteriores.

## Parámetros
- **Step** – distancia en pips entre el precio de mercado y las órdenes límite pendientes.
- **Stop Loss** – tamaño del stop protector en pips para posiciones abiertas.
- **Take Profit** – tamaño del objetivo de ganancia en pips para posiciones abiertas.
- **Use Martingale** – habilita el incremento de volumen tras una operación perdedora.
- **Loss Limit** – número máximo de operaciones perdedoras consecutivas antes de restablecer el volumen.
- **Volume** – volumen inicial de la orden.
- **Use MegaLot** – duplica el volumen en lugar de agregar el volumen base cuando el martingala está activo.
- **Candle Type** – tipo de datos de velas usado para el procesamiento.

## Lógica de operación
1. Cuando no hay posición abierta ni orden activa, la estrategia coloca una orden Buy Limit por debajo del último cierre y una orden Sell Limit por encima, ambas a la distancia `Step` especificada.
2. Tras la ejecución de una orden, la orden pendiente opuesta permanece, permitiendo solo una posición activa a la vez.
3. La posición se cierra cuando se alcanza el nivel de stop loss o take profit.
4. Tras una operación perdedora, el volumen de posición puede incrementarse según la configuración del martingala.

## Notas
- La estrategia utiliza la API de alto nivel de StockSharp con el enfoque `Bind` para el manejo de datos de velas.
- Todos los comentarios dentro del código están escritos en inglés para cumplir las convenciones del repositorio.
