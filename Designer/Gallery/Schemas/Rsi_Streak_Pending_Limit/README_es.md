# Diagrama de límites pendientes por racha de RSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama espera a que el RSI permanezca en una zona extrema antes de colocar una orden limitada de retroceso. Confirma el RSI actual y sus dos valores finalizados anteriores, admite una orden pendiente por cada visita continua a la zona, cancela la orden cuando el RSI abandona esa zona y protege cada ejecución con salidas porcentuales.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas finalizadas de cinco minutos alimentan un RSI de longitud 14 y proporcionan el cierre utilizado para calcular las órdenes pendientes.
- La zona inferior está por debajo de 30 y la superior por encima de 70. Un bloque N values programa la evaluación tras tres actualizaciones finalizadas del RSI.
- En la evaluación, los bloques Formula comprueban conjuntamente el RSI actual, los dos valores anteriores y la restricción de posición. Una secuencia interrumpida no genera entrada.
- La compra limitada se coloca un 0,2% por debajo del cierre de señal; la venta limitada, un 0,2% por encima.
- Dos bloques Flag independientes permiten una sola orden durante cada permanencia continua en una zona extrema.
- Las entradas ejecutadas reciben un objetivo del 1,5% y un stop del 1%; ambos envían una orden de mercado al activarse.

## Reglas de entrada y salida

- **Entrada en largo**: Tras la evaluación de tres actualizaciones, los tres valores del RSI deben estar por debajo de 30 y Position debe ser menor o igual que cero. Se registra una compra limitada en `Close × (1 − Pending Offset / 100)`.
- **Entrada en corto**: Tras la evaluación de tres actualizaciones, los tres valores del RSI deben estar por encima de 70 y Position debe ser mayor o igual que cero. Se registra una venta limitada en `Close × (1 + Pending Offset / 100)`.
- **Orden pendiente**: Una compra no ejecutada se cancela cuando el RSI supera 30; una venta no ejecutada se cancela cuando el RSI cae por debajo de 70. El mismo evento reinicia el Flag de ese lado para una visita posterior.
- **Salida**: Cuando se ejecuta una orden pendiente, Position protection cierra su exposición con un movimiento favorable del 1,5% o desfavorable del 1%, mediante una orden de mercado.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Velas | 00:05:00 | Marco temporal de las velas finalizadas empleadas para señales y precios. |
| Longitud del RSI | 14 | Longitud de promediado del índice de fuerza relativa. |
| Campo de entrada del RSI | Sin definir | No se selecciona un campo de entrada alternativo para el indicador. |
| Sobreventa | 30 | Límite superior estricto de una secuencia larga de tres valores. |
| Sobrecompra | 70 | Límite inferior estricto de una secuencia corta de tres valores. |
| Número de coincidencias (N) | 3 | Cantidad de actualizaciones finalizadas del RSI en el intervalo de confirmación. |
| Separación pendiente, % | 0.2 | Distancia entre el límite y el cierre de la vela de señal. |
| Volumen | 1 | Tamaño de cada orden pendiente de entrada. |
| Objetivo, % | 1.5 | Movimiento favorable desde la entrada ejecutada que activa la protección. |
| Stop, % | 1 | Movimiento desfavorable desde la entrada ejecutada que activa la protección. |
| Stop dinámico | false | Mantiene fijo el límite del stop. |
| Usar órdenes de mercado | true | Envía las salidas protectoras activadas como órdenes de mercado. |

## Detalles del diagrama

- El RSI actual alimenta dos bloques Previous value con desplazamientos 1 y 2. Los tres valores proceden únicamente de velas finalizadas.
- Un bloque N values compartido se arma desde cualquiera de las zonas extremas y cuenta tres actualizaciones del RSI. Su salida toma ambas puntuaciones de entrada en el mismo punto de evaluación.
- La puntuación larga solo es negativa si los tres RSI están por debajo de 30 y Position no es positiva. La puntuación corta solo es negativa si los tres valores superan 70 y Position no es negativa.
- Si el RSI abandona la zona durante el intervalo de confirmación, la puntuación correspondiente no es negativa y no se produce un disparo de registro. Otra visita extrema puede iniciar un intervalo nuevo.
- Los bloques Formula calculan ambos límites desde el cierre y los bloques Order registering envían órdenes limitadas sin redondear de una unidad.
- Order cancellation conserva la orden más reciente de cada lado y actúa en cuanto el RSI vuelve a cruzar el límite de esa zona.
- Una orden de una unidad contra una posición opuesta de una unidad primero deja la exposición en cero; se necesita otra visita confirmada para abrir exposición en la nueva dirección.
- El gráfico muestra velas de cinco minutos, RSI con ambos niveles, los dos flujos de órdenes pendientes y todas las ejecuciones o salidas de la estrategia.

## Uso

Importe el archivo `.json` en Designer, ejecútelo con datos históricos en el probador y ajuste los niveles del RSI, la separación pendiente, las distancias de protección y el volumen al instrumento antes de operar en real.
