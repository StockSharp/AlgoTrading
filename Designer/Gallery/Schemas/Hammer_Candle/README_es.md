# Diagrama de la estrategia de martillo e invertido con filtro SMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un martillo es una vela de cuerpo pequeño, sombra inferior larga y prácticamente sin sombra superior: dentro de la barra el precio fue empujado muy abajo y recomprado antes del cierre. El martillo invertido es su imagen especular. Por sí solas estas formas aparecen por todas partes, así que una media móvil simple decide dónde merece la pena tomarlas: el martillo solo se compra por debajo de la media y el invertido solo se vende por encima.

![schema](schema.svg)

## Resumen de la estrategia

- Dos bloques de patrón de velas llevan exactamente las fórmulas de la estrategia original: cuerpo mayor que cero, una sombra más larga que el doble del cuerpo y la sombra opuesta más corta que la mitad del cuerpo.
- Los patrones integrados Hammer e Inverted Hammer se descartan a propósito, porque miden las sombras contra la longitud de la vela y no contra el cuerpo.
- La media móvil simple del precio de cierre parte el gráfico en una mitad barata y otra cara, y es a la vez filtro de entrada y línea de salida.
- El control de la posición garantiza que un patrón solo se opere estando plano.

## Reglas de entrada y salida

- **Entrada en largo**: El bloque de patrón informa de un martillo, la vela cerró por debajo de la media móvil y la posición está plana. La orden compra un lote y abre un largo.
- **Entrada en corto**: El bloque de patrón informa de un martillo invertido, la vela cerró por encima de la media móvil y la posición está plana. La orden vende un lote y abre un corto.
- **Salida**: El largo se cierra cuando una vela cierra por encima de la media móvil y el corto cuando cierra por debajo, ambos mediante bloques de modificación de posición en modo cierre. La estrategia original sale por el mismo lado de la media por el que entró y sostiene la operación con una pausa de varios cientos de barras; aquí no hay bloque contador de barras, de modo que copiar esa salida al pie de la letra cerraría cada operación en la vela siguiente. La vuelta a la media es la regla más cercana que aún mantiene la posición un tramo razonable.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| SMA Length | 20 | Periodo de suavizado de la media móvil simple que filtra los patrones y cierra las operaciones. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta los dos bloques de patrón, la media móvil y un convertidor que extrae el precio de cierre de la vela.
- Dos bloques de comparación enfrentan ese cierre con la media y se reutilizan dos veces cada uno: como filtro de entrada de un lado y como disparador de salida del otro.
- El bloque de posición se compara con una constante cero y cada Y lógica une el patrón, el lado de la media y esa protección.
- Ambos bloques de entrada envían órdenes a mercado y toman el volumen de una constante compartida; los dos bloques de salida trabajan en modo cierre y solo actúan cuando hay algo que cerrar.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
