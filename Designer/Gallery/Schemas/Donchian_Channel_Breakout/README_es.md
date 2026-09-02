# Diagrama de la estrategia de ruptura del canal de Donchian
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La idea de seguimiento de tendencia más antigua que existe: el indicador Donchian Channels dibuja el máximo más alto y el mínimo más bajo de las últimas N velas, y la estrategia se suma al movimiento en cuanto una vela cierra fuera del canal. Siempre está en el mercado y gira de largo a corto, y viceversa, con la ruptura contraria.

![schema](schema.svg)

## Resumen de la estrategia

- Los Donchian Channels se calculan sobre velas cerradas: la banda superior es el máximo del periodo y la inferior, el mínimo.
- Ambas bandas se retrasan una vela, de modo que el cierre actual se compara con un canal ya cerrado; de lo contrario, la propia vela elevaría la banda que debe romper.
- La posición actual interviene en cada decisión y al volumen de la orden se le suma el valor absoluto de la posición, así una sola orden a mercado cierra el lado antiguo y abre el nuevo.

## Reglas de entrada y salida

- **Entrada en largo**: La vela cierra por encima de la banda superior de la vela anterior y la posición no es larga. La orden compra el volumen base más el valor absoluto de la posición: gira un corto a largo o abre un largo desde plano.
- **Entrada en corto**: La vela cierra por debajo de la banda inferior de la vela anterior y la posición no es corta. La orden vende el volumen base más el valor absoluto de la posición: gira un largo a corto o abre un corto desde plano.
- **Salida**: No hay stop, ni objetivo, ni bloque de salida propio: la posición se mantiene hasta que la ruptura contraria la gira, igual que en la estrategia original.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Channel period | 20 | Número de velas sobre las que se toman el máximo y el mínimo. |
| Volume | 1 | Volumen base de la orden, en lotes; al girar se le añade el valor absoluto de la posición. |
| Candles | 00:05:00 | Marco temporal de las velas con el que trabaja todo el diagrama. |

## Detalles del diagrama

- El bloque de velas alimenta el indicador Donchian Channels y, mediante un conversor, el precio de cierre.
- Dos conversores extraen del indicador los valores UpperBand y LowerBand, y dos bloques de valor anterior los desplazan una vela atrás.
- Dos bloques de comparación contrastan el cierre con las bandas desplazadas; otros dos comparan la posición con cero, y una Y lógica reúne una condición de cada tipo en la señal de entrada.
- Un bloque de fórmula calcula el volumen de giro como volumen base más el valor absoluto de la posición y lo envía a los dos bloques de modificación de posición.
- El código original usa por defecto un canal de 1000 velas de un minuto; el diagrama emplea un canal de 20 velas de cinco minutos, el valor que describen el README de la estrategia y su rango de optimización, para que realmente opere con un mes de historial.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
