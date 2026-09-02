# Diagrama de la estrategia de ruptura por volatilidad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Un canal construido a mano: la media móvil simple da el centro, el Average True Range da la anchura, y un cierre fuera de SMA más o menos un múltiplo del ATR se toma como un movimiento al que merece la pena sumarse. Como el canal respira con la volatilidad, el mismo multiplicador sigue teniendo sentido en mercados tranquilos y rápidos.

![schema](schema.svg)

## Resumen de la estrategia

- SMA y ATR usan el mismo periodo sobre velas cerradas, de modo que el canal se centra en el precio medio y se escala con el rango verdadero reciente.
- Dos bloques de fórmula construyen los bordes: el superior es SMA más multiplicador por ATR y el inferior, SMA menos esa misma cantidad.
- La estrategia siempre está en el mercado: la ruptura contraria gira la posición y un stop de protección la cierra antes si el movimiento falla.

## Reglas de entrada y salida

- **Entrada en largo**: La vela cierra por encima de SMA más multiplicador por ATR y la posición no es larga. La orden compra el volumen base más el valor absoluto de la posición: gira un corto a largo o abre un largo desde plano.
- **Entrada en corto**: La vela cierra por debajo de SMA menos multiplicador por ATR y la posición no es corta. La orden vende el volumen base más el valor absoluto de la posición: gira un largo a corto o abre un corto desde plano.
- **Salida**: No hay salida basada en indicadores. La posición la gira la ruptura contraria, o la cierra antes el bloque de protección con stop de pérdidas conectado a las operaciones de ambas entradas.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Indicator period | 20 | Periodo compartido por la SMA que centra el canal y por el ATR que fija su anchura. |
| ATR multiplier | 2 | A cuántos ATR de la media móvil se sitúa el borde de ruptura. |
| Stop loss, % | 2 | Stop de pérdidas de protección, en porcentaje del precio de entrada. |
| Volume | 1 | Volumen base de la orden, en lotes; al girar se añade el valor absoluto de la posición. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta los dos indicadores y, mediante un conversor, el precio de cierre que sirve tanto para las comparaciones como de fuente de precio del bloque de protección.
- Una constante guarda el multiplicador y dos bloques de fórmula calculan el borde superior y el inferior a partir de SMA, el multiplicador y el ATR.
- Dos bloques de comparación contrastan el cierre con los bordes, otros dos comparan la posición con cero y cada Y lógica reúne una condición de cada tipo en una entrada.
- Un bloque de fórmula calcula el volumen de giro como volumen base más el valor absoluto de la posición y alimenta los dos bloques de modificación de posición.
- El original protege la posición con un stop de dos unidades absolutas de precio, calibrado para otro instrumento y que saltaría de inmediato en un precio de cripto; el diagrama usa en su lugar un stop del dos por ciento, que se comporta como pretendía el original en cualquier instrumento.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
