# Diagrama de la estrategia Larry Connors 3 Day High/Low
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El 3 Day High/Low de Larry Connors compra un retroceso breve dentro de un mercado alcista. El precio debe mantenerse por encima de una SimpleMovingAverage lenta, caer por debajo de una rápida y dibujar tres velas seguidas cuyo máximo y mínimo sean menores que los de la vela anterior. La operación se entrega en el primer cierre por encima de la media rápida. El original trabaja con velas diarias; este diagrama usa velas de cinco minutos para ajustarse al histórico intradía incluido.

![schema](schema.svg)

## Resumen de la estrategia

- Un bloque de patrón de velas contiene toda la figura de cuatro velas: tres consecutivas, cada una con máximo y mínimo menores que la anterior.
- Una SimpleMovingAverage de 50 periodos determina que el mercado sube, de modo que el retroceso solo se compra a favor del movimiento mayor.
- Una SimpleMovingAverage de 5 periodos es a la vez la puerta de entrada, ya que el precio por debajo indica que el retroceso continúa, y el disparador de salida.
- La estrategia es solo larga. El original además limita el número de entradas y espera quince barras entre operaciones; no existe un bloque contador, así que este diagrama opera con más frecuencia que la fuente.

## Reglas de entrada y salida

- **Entrada en largo**: El bloque de patrón informa de tres máximos y mínimos descendentes, el cierre está por encima de la SMA lenta, el cierre está por debajo de la SMA rápida y la posición es plana. La orden compra el volumen compartido a mercado y abre el largo.
- **Entrada en corto**: No hay lado corto. Las reglas de Connors solo compran retrocesos dentro de un mercado alcista, por lo que el diagrama carece de entrada vendedora.
- **Salida**: El primer cierre por encima de la SMA rápida cierra el largo. El bloque de cierre envía una orden a mercado por el tamaño abierto y, igual que en el código original, no hay stop de pérdidas ni objetivo de beneficio.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Slow SMA Length | 50 | Periodo de la SimpleMovingAverage lenta, el filtro de mercado alcista. |
| Fast SMA Length | 5 | Periodo de la SimpleMovingAverage rápida: el precio por debajo abre la operación y el primer cierre por encima la cierra. |
| Volume | 1 | Volumen de la orden, en lotes. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el indicador de patrón, ambas medias móviles y un conversor que toma el precio de cierre.
- Dos bloques de comparación enfrentan el cierre con las dos medias y el bloque de posición se compara con una constante cero.
- Una Y lógica une la señal del patrón, las dos condiciones de medias y la comprobación de posición plana, y dispara un bloque de modificación de posición en modo apertura.
- Un segundo bloque de modificación, en modo cierre, se activa cuando el cierre vuelve por encima de la media rápida; no necesita volumen porque cierra lo que haya abierto.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
