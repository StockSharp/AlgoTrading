# Diagrama de la estrategia de cruce de media con filtro ADX
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama opera la vela que pisa una media móvil simple larga, pero solo mientras el ADX confirma que el mercado realmente tiene tendencia. Una vela cuenta como cruce si abrió a un lado de la media y cerró al otro, y entonces la posición se gira hacia el lado del cierre. El original trabaja con velas de un minuto; este diagrama usa las velas de cinco minutos del histórico incluido.

![schema](schema.svg)

## Resumen de la estrategia

- La SMA de 200 es la línea de referencia y un bloque de valor anterior guarda el valor que tenía una vela antes, de modo que la apertura se mide contra la media de su propia barra y el cierre contra la actual.
- El O exclusivo de esas dos comparaciones es cierto exactamente en las barras que cabalgan la media: así define el cruce el código original, y no como el cruce de dos líneas de indicadores.
- El ADX de longitud cincuenta filtra cada entrada: una vela que cruza la media en un mercado tranquilo se ignora.
- No hay stop ni objetivo: la posición solo se gira con el cruce contrario, y el volumen de la orden es el volumen compartido más lo que ya se tiene.

## Reglas de entrada y salida

- **Entrada en largo**: El ADX supera el umbral, la vela cruzó la media, el cierre está por encima de la SMA actual y la posición no es larga. La orden compra el volumen compartido más el tamaño del corto abierto, así que una sola orden cierra el corto y abre el largo.
- **Entrada en corto**: El ADX supera el umbral, la vela cruzó la media, el cierre está en la SMA actual o por debajo y la posición no es corta. La orden vende el volumen compartido más el tamaño del largo abierto.
- **Salida**: No hay salida propia: la posición se mantiene hasta que el cruce contrario la gira, igual que en el código original, que no implementa ni stop loss ni take profit.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| ADX Length | 50 | Periodo de suavizado del índice direccional medio. |
| ADX Threshold | 25 | Valor de ADX que el mercado debe superar para permitir una entrada. |
| SMA Length | 200 | Periodo de la media móvil simple contra la que se miden las velas. |
| Volume | 1 | Volumen de la orden, en lotes, antes de sumarle la posición abierta. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- Dos conversores leen la apertura y el cierre de cada vela terminada, mientras que la media móvil y el ADX se calculan sobre la propia vela.
- Un bloque de valor anterior retrasa la SMA una vela; las dos comparaciones que usan el valor viejo y el actual se unen con un O exclusivo, que es la prueba de cruce.
- Un NO lógico convierte la condición «cierre por encima de la media» en la condición del lado corto, así una sola comparación sirve para ambos sentidos.
- Un bloque de fórmula suma la posición absoluta al volumen compartido, lo que permite que una orden a mercado cierre el lado viejo y abra el nuevo de una vez.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
