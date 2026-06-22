# Estrategia N Candles v3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia escanea las últimas velas terminadas y busca una secuencia donde las últimas *N* barras comparten la misma dirección (todas alcistas o todas bajistas). Cuando aparece tal racha, entra en la dirección de la secuencia mientras respeta un límite sobre cuántas posiciones se pueden abrir a la vez. La implementación migra el asesor experto original de MetaTrader 5 a la API de alto nivel de StockSharp.

## Lógica de trading
- El motor se suscribe al tipo de vela configurado y procesa solo las barras completadas.
- Para cada vela terminada se evalúa la dirección del cuerpo: alcista, bajista o neutral (doji).
- Las velas doji reinician el contador interno. De lo contrario, el contador aumenta cuando la vela actual tiene la misma dirección que las anteriores. Una vez que el contador alcanza el parámetro `Identical Candles`, la estrategia emite una nueva orden.
- Las señales largas cierran primero cualquier exposición corta existente y luego añaden una unidad larga mientras el volumen comprado total se mantenga por debajo de `Max Positions * Volume`.
- Las señales cortas funcionan simétricamente para las rachas bajistas.

## Gestión de riesgo
- Después de cada operación ejecutada, la estrategia coloca nuevas órdenes de stop-loss y take-profit de protección basadas en el precio de entrada promedio de la posición activa.
- Las distancias se miden en pasos de precio del instrumento: `Take Profit Points` multiplica el paso para calcular el objetivo por encima (largo) o por debajo (corto) de la entrada; `Stop Loss Points` usa la misma idea para el stop de protección.
- Un trailing stop escalonado puede reemplazar el stop inicial una vez que el precio se mueva `Trailing Stop Points` a favor de la posición. El stop se mueve solo cuando el precio ha avanzado al menos `Trailing Step Points` más allá del nivel de trailing anterior.

## Parámetros
- **Candle Type** – Marco temporal o fuente de velas a analizar.
- **Identical Candles** – Número requerido de velas consecutivas con la misma dirección para disparar una entrada.
- **Volume** – Tamaño de la orden para cada nueva entrada en unidades del instrumento.
- **Max Positions** – Número máximo de unidades de entrada que pueden estar abiertas en la misma dirección simultáneamente.
- **Take Profit Points** – Distancia del take-profit en múltiplos del paso de precio del instrumento.
- **Stop Loss Points** – Distancia del stop-loss en múltiplos del paso de precio del instrumento.
- **Trailing Stop Points** – Distancia desde el precio actual usada para activar y mantener el trailing stop. Establecer en cero para deshabilitar el trailing.
- **Trailing Step Points** – Distancia extra en pasos de precio que debe cubrirse antes de mover nuevamente el trailing stop.

## Notas adicionales
- La estrategia opera de manera neteada: cuando aparece una señal en dirección opuesta, cualquier exposición existente del otro lado se cierra antes de añadir una nueva posición.
- Todas las órdenes protectoras se recrean después de cada ejecución para mantener su volumen sincronizado con el tamaño de la posición abierta.
- Asegurarse de que el instrumento proporcione un `PriceStep` distinto de cero; de lo contrario, se usa el valor de paso predeterminado de 1.
