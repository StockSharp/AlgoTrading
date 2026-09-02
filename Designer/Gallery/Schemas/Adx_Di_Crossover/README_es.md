# Diagrama de la estrategia de cruce de ADX y líneas DI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El sistema de movimiento direccional de Welles Wilder reunido en un solo diagrama. El bloque Average Directional Index entrega tres números a la vez: la línea +DI, la línea -DI y la propia línea ADX. El cruce de las líneas direccionales elige el lado de la operación, mientras que la línea ADX decide si el mercado tiene tendencia suficiente para entrar.

![schema](schema.svg)

## Resumen de la estrategia

- Un único bloque AverageDirectionalIndex alimenta tres conversores que extraen +DI, -DI y la línea ADX del mismo valor complejo del indicador.
- El bloque de cruce vigila +DI frente a -DI y solo dispara en la vela en la que ambas líneas intercambian posiciones.
- La línea ADX debe situarse en el umbral o por encima, de modo que los tramos laterales y sin dirección quedan filtrados.
- Un bloque de fórmula suma el valor absoluto de la posición al volumen base, así una sola orden a mercado cierra el lado antiguo y abre el nuevo.

## Reglas de entrada y salida

- **Entrada en largo**: +DI cruza al alza por encima de -DI, la línea ADX está en el umbral o por encima y la posición todavía no es larga. La orden compra el volumen base más el tamaño del corto: da la vuelta a un corto o abre un largo desde plano.
- **Entrada en corto**: +DI cruza a la baja por debajo de -DI, la línea ADX está en el umbral o por encima y la posición todavía no es corta. La orden vende el volumen base más el tamaño del largo: da la vuelta a un largo o abre un corto desde plano.
- **Salida**: No hay bloque de salida propio. La posición vive hasta el cruce contrario de las líneas DI, y la orden de vuelta la cierra y abre la contraria en un solo paso.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| ADX Period | 14 | Periodo de suavizado compartido por la línea ADX y por el par +DI/-DI. |
| ADX Threshold | 15 | Lectura mínima de ADX que se considera una tendencia operable. |
| Volume | 1 | Volumen base de la orden, en lotes; encima se suma el tamaño de la posición abierta. |
| Candles | 00:15:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el bloque de indicador y tres conversores extraen Dx.Plus, Dx.Minus y MovingAverage de su valor.
- El bloque de cruce emite verdadero cuando +DI pasa por encima de -DI y falso cuando pasa por debajo, así un NO lógico convierte la misma salida en la señal corta.
- Una comparación contrasta la línea ADX con la constante de umbral; otras dos comparan la posición con cero, una por lado.
- Cada Y lógica une el cruce, el filtro de tendencia y el control de posición, y dispara un bloque de modificación de posición cuyo volumen viene del bloque de fórmula.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
