# Diagrama de la estrategia de cruce de niveles del MFI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El índice de flujo de dinero pondera cada movimiento del precio por el volumen que lo acompaña, así que indica cuánto dinero empuja realmente al mercado. Este diagrama opera contra los extremos: compra en la vela en la que el MFI baja atravesando el nivel inferior hacia la zona de sobreventa y vende en la vela en la que sube atravesando el nivel superior hacia la zona de sobrecompra. Un take profit y un stop loss porcentuales cierran cada operación.

![schema](schema.svg)

## Resumen de la estrategia

- El Money Flow Index de longitud 14 se calcula sobre velas horarias cerradas, que el probador construye a partir del histórico de cinco minutos incluido.
- Los niveles 30 y 70 se leen como cruces y no como zonas: solo la vela que entra en una zona genera señal, no las que permanecen dentro.
- La estrategia original tiene un interruptor Trend que puede invertir ambas señales; el diagrama conserva el modo Direct por defecto, de modo que entrar en sobreventa compra y entrar en sobrecompra vende.
- La posición actual interviene en las dos decisiones, así que el esquema nunca añade una segunda orden a una posición ya abierta.

## Reglas de entrada y salida

- **Entrada en largo**: El valor anterior del MFI estaba por encima del nivel inferior y el actual está en él o por debajo, y la posición no es larga. La orden compra un lote: abre un largo desde plano o cierra un corto existente.
- **Entrada en corto**: El valor anterior del MFI estaba por debajo del nivel superior y el actual está en él o por encima, y la posición no es corta. La orden vende un lote: abre un corto desde plano o cierra un largo existente.
- **Salida**: El bloque de protección cierra la operación con un take profit del 2 por ciento o un stop loss del 1 por ciento sobre el precio de entrada; antes de eso, el cruce contrario deja la posición en cero, porque todas las órdenes usan el mismo volumen.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| MFI Length | 14 | Periodo de suavizado del Money Flow Index. |
| Low Level | 30 | Nivel que el indicador debe atravesar hacia abajo para habilitar una compra. |
| High Level | 70 | Nivel que el indicador debe atravesar hacia arriba para habilitar una venta. |
| Take profit, % | 2 | Distancia del take profit respecto al precio de entrada, en porcentaje. |
| Stop loss, % | 1 | Distancia del stop loss respecto al precio de entrada, en porcentaje. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 01:00:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque de indicador con el Money Flow Index, y un bloque de valor anterior guarda la lectura de una vela atrás.
- Cuatro bloques de comparación arman los dos cruces: anterior por encima del nivel más actual en él o por debajo para el lado largo; anterior por debajo más actual en él o por encima para el lado corto.
- Otros dos bloques de comparación contrastan la posición con una constante cero, y cada Y lógica une un cruce con su control de posición.
- Ambos bloques de modificación envían órdenes a mercado con el volumen de una constante compartida, y sus operaciones alimentan el bloque de protección que lleva el take profit y el stop loss.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
