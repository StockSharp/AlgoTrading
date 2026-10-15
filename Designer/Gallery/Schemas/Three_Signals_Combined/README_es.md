# Diagrama de estrategia con entrada MACD y promediado combinados
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Este diagrama combina un cruce nuevo de la diferencia de EMA y un evento de promediado basado en precio dentro de un único flujo de entradas largas. Cada compra es de una unidad, cada ciclo admite como máximo cinco entradas y una recuperación del dos por ciento sobre la última ejecución cierra todas las unidades contabilizadas.

![schema](schema.svg)

## Resumen de la estrategia

- Las velas terminadas de cinco minutos alimentan EMA(12), EMA(26) ya formadas y el precio de cierre.
- La fórmula `Fast EMA - Slow EMA` crea la línea MACD empleada por el diagrama; Crossing detecta su cruce ascendente de cero.
- El cruce ascendente abre el primer largo solo cuando Position no es positiva y el contador de entradas vale cero.
- Mientras hay un largo, un cierre al menos un cinco por ciento por debajo de la última ejecución añade una unidad si se han contado menos de cinco entradas.
- Combination reúne exactamente esos dos eventos booleanos y activa un único bloque Buy a mercado.
- Un cierre al menos un dos por ciento por encima de la última ejecución vende todo el tamaño contabilizado. Después se reinicia el contador para el siguiente ciclo.

## Reglas de entrada y salida

- **Entrada larga inicial**: EMA(12) menos EMA(26) cruza cero hacia arriba, Position es menor o igual que cero y Entries in Current Long vale cero. Se compra una unidad a mercado.
- **Entrada de promediado**: Position es positiva, el contador está por debajo de Maximum Entries y la vela terminada cierra en o por debajo de `Latest Entry Fill × (1 - Averaging Drop / 100)`. Se compra una unidad adicional a mercado.
- **Salida**: con Position positiva, una vela terminada cierra en o por encima de `Latest Entry Fill × (1 + Take Profit / 100)`. Se vende a mercado toda la cantidad contabilizada y se reinicia el contador.
- **Alcance**: el diagrama solo opera largos y administra las unidades abiertas por su propio flujo de entradas. No hay entrada corta ni stop-loss.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|---|---|---|
| Serie de velas | 00:05:00 | Velas terminadas de cinco minutos usadas en todas las decisiones. |
| Longitud EMA rápida | 12 | Longitud de la media móvil exponencial rápida. |
| Campo de EMA rápida | Sin definir | No se ha seleccionado otro campo de entrada del indicador. |
| Longitud EMA lenta | 26 | Longitud de la media móvil exponencial lenta. |
| Campo de EMA lenta | Sin definir | No se ha seleccionado otro campo de entrada del indicador. |
| Entradas máximas | 5 | Número máximo de compras de una unidad en un ciclo largo. |
| Caída para promediar | 5 | Descenso porcentual desde la última ejecución necesario para otra compra. |
| Toma de beneficio | 2 | Subida porcentual desde la última ejecución necesaria para la salida completa. |
| Volumen de entrada | 1 | Volumen fijo a mercado de cada compra inicial o de promediado. |

## Detalles del diagrama

- Candles emite únicamente valores terminados y puede construir la serie de cinco minutos con velas almacenadas de menor intervalo.
- Ambos bloques EMA emiten solo valores formados. La primera diferencia MACD utilizable aparece tras el calentamiento de la EMA lenta.
- La fórmula `a - b` recibe las EMA rápida y lenta, y Crossing compara el resultado con la Variable de cero.
- Position proporciona las comprobaciones de dirección, mientras Entries in Current Long aplica el requisito de contador cero y el límite de cinco entradas.
- Cada ejecución Buy entrega su precio medio de orden a Latest Entry Fill. Como cada orden de entrada se ejecuta una sola vez, ese valor es el precio usado por ambos niveles porcentuales.
- Una ruta retardada suma el volumen ejecutado después de cada Buy. La ejecución de la salida completa devuelve cero al mismo contador antes de la siguiente decisión.
- Combination es booleano y tiene dos entradas conectadas: Fresh MACD Entry y Averaging Entry Below Step Five. Entry Volume se publica después de ambas ramas para consumir el resultado actual.
- El gráfico recibe velas, ambas EMA, la línea MACD, el último precio de entrada, el nivel de promediado, el nivel de beneficio y todas las ejecuciones.

## Uso

Importe `Three_Signals_Combined.json` en Designer, aporte historial suficiente para formar EMA(26) y pruebe la configuración de cinco minutos con el instrumento elegido. Evalúe la exposición de cinco entradas y la ausencia de stop-loss antes de operar en vivo.
