# Estrategia CandlesticksBW
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el enfoque CandlesticksBW de Bill Williams. Colorea cada vela usando el momentum del Awesome Oscillator (AO) y el Accelerator Oscillator (AC). La estrategia abre o cierra posiciones según las transiciones entre colores alcistas y bajistas.

## Cómo funciona
- Calcula el AO como la diferencia entre las SMA de 5 y 34 períodos del precio mediano.
- Calcula el AC como AO menos la SMA de 5 períodos del AO.
- Cada vela se clasifica en seis colores dependiendo del crecimiento de AO/AC y la dirección de la vela.
- Una configuración alcista ocurre cuando la penúltima vela es alcista (color 0 o 1). Si el color de la última vela es superior a 1, se abre una posición larga y se cierran las posiciones cortas.
- Una configuración bajista ocurre cuando la penúltima vela es bajista (color 4 o 5). Si el color de la última vela es inferior a 4, se abre una posición corta y se cierran las posiciones largas.
- Los stops y objetivos se aplican a través de `StartProtection`.

## Parámetros
- `CandleType` – marco temporal de las velas.
- `SignalBar` – barra de desplazamiento para la evaluación de señales.
- `StopLoss` – distancia del stop loss en puntos.
- `TakeProfit` – distancia del take profit en puntos.
- `BuyPosOpen` – permitir abrir posiciones largas.
- `SellPosOpen` – permitir abrir posiciones cortas.
- `BuyPosClose` – permitir cerrar posiciones largas.
- `SellPosClose` – permitir cerrar posiciones cortas.

## Indicadores
- Awesome Oscillator (derivado de SMA).
- Accelerator Oscillator.

## Reglas de operación
- **Entrada larga:** color de la penúltima vela <2 y último color >1.
- **Entrada corta:** color de la penúltima vela >3 y último color <4.
- **Salida larga:** en condición de entrada corta si posición >0.
- **Salida corta:** en condición de entrada larga si posición <0.
