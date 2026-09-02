# Diagrama de la estrategia del canal de regresión lineal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Se ajusta una recta por mínimos cuadrados sobre los últimos cincuenta cierres y se dibuja un canal a su alrededor con una amplitud de varios errores estándar de la regresión. El precio fuera del canal se considera un movimiento estirado y la estrategia lo devuelve hacia la recta mientras la pendiente del canal juegue a su favor.

![schema](schema.svg)

## Resumen de la estrategia

- LinearReg da el valor de la recta ajustada en la barra actual, LinearRegSlope su dirección y StandardError la dispersión habitual de los cierres alrededor de ella.
- Las bandas son la recta más y menos el multiplicador de desviación por el error estándar, de modo que el canal se ensancha y se estrecha solo con el mercado.
- La pendiente actúa de filtro: una caída solo se compra dentro de un canal ascendente y un pico solo se vende dentro de uno descendente.
- La recta de regresión es el objetivo; no hay stop de pérdidas ni toma de beneficios, igual que en la estrategia de origen.

## Reglas de entrada y salida

- **Entrada en largo**: La pendiente de la regresión es mayor que cero, el cierre queda por debajo de la banda inferior y la posición está plana. La orden de compra abre un largo de un lote.
- **Entrada en corto**: La pendiente de la regresión es menor que cero, el cierre queda por encima de la banda superior y la posición está plana. La orden de venta abre un corto de un lote.
- **Salida**: El largo se cierra en cuanto el cierre alcanza la recta desde abajo y el corto en cuanto la alcanza desde arriba. Ambos bloques de salida trabajan en modo cierre de posición, así que no hacen nada si no hay posición.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| LinearReg Length | 50 | Número de velas sobre las que se ajusta la recta de regresión. |
| LinearRegSlope Length | 50 | Número de velas para medir la pendiente; manténgalo igual a la longitud de la recta. |
| StandardError Length | 50 | Número de velas para medir el error estándar; manténgalo igual a la longitud de la recta. |
| Channel Deviation | 1.5 | Semiamplitud del canal, en errores estándar de la regresión. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Un solo bloque de velas alimenta tres indicadores y un conversor del precio de cierre, así que todos los valores del diagrama proceden de la misma vela cerrada.
- Dos bloques de fórmula construyen las bandas a partir de la recta, el error estándar y una constante de desviación compartida que se puede optimizar.
- Seis bloques de comparación convierten esos números en señales: dos para la pendiente, dos para las bandas y dos para el regreso a la recta.
- Cada entrada es una Y lógica de pendiente, banda y posición plana; las salidas van directamente de su comparación a un bloque de cierre de posición.
- La estrategia original espera veinte barras entre operaciones y calcula la desviación sobre toda la ventana, mientras que StandardError divide entre la ventana menos dos, lo que ensancha el canal alrededor de un dos por ciento; baje la desviación a unos 1,47 para reproducir la banda original.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
