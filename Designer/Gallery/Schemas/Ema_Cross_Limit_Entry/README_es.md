# Entrada limitada perseguida por cruce EMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama conserva el cruce EMA(12)/EMA(26) y la confirmación Momentum(10) de Franks4HourLimitOrdersStrategy, pero hace explícita la ejecución: el límite nace en el cierre, sigue cierres posteriores mediante Order replacing y se cancela en el cruce contrario.

![schema](schema.svg)

## Resumen de la estrategia

- Velas terminadas alimentan dos EMA y Momentum; Crossing solo emite cuando cambia realmente el orden de las medias.
- El cruce alcista requiere Momentum positivo y Position <= 0; el bajista, Momentum negativo y Position >= 0.
- Order registering coloca el primer límite al cierre de la vela señal sin ajustar al paso de precio.
- Combination conserva la orden más reciente devuelta por Order replacing para actualizar y cancelar el objeto vivo.
- La sustitución exige que EMA y Momentum sigan validando el lado y no se ejecuta incondicionalmente.

## Reglas de entrada y salida

- **Entrada en largo**: EMA(12) cruza sobre EMA(26), Momentum es positivo y Position está plana o corta. La compra limitada al cierre usa abs(Position)+1 para cerrar el corto y abrir largo con una reversión neta.
- **Entrada en corto**: EMA(12) cruza bajo EMA(26), Momentum es negativo y Position está plana o larga. La venta limitada usa el mismo tamaño de reversión neta.
- **Salida**: El cruce contrario cancela el límite pendiente y puede enviar el opuesto. La posición ejecutada también recibe stop 1% y objetivo 3% añadidos por el diagrama; una salida protectora cancela referencias pendientes.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo terminado: cinco minutos en la galería y cuatro horas por defecto en C#. |
| Fast EMA Length | 12 | Cantidad de valores en la ExponentialMovingAverage rápida. |
| Slow EMA Length | 26 | Cantidad de valores en la ExponentialMovingAverage lenta. |
| Momentum Length | 10 | Cantidad de valores de Momentum cuyo signo confirma el cruce. |

## Detalles del diagrama

- El C# entra a mercado y no gestiona pendientes; registro, sustitución y cancelación son la adaptación de ejecución mostrada aquí.
- El límite no ejecutado sigue cada cierre solo mientras orden EMA, signo Momentum y lado de Position mantienen el setup.
- El código usa cuatro horas por defecto. El ejemplo usa cinco minutos porque un mes H4 apenas forma EMA(26); el parámetro permite 04:00:00.
- Volumen base 1 y Position protection con stop 1% / objetivo 3% son adiciones fijas, no parámetros del constructor.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
