# Estrategia de Scalping Angry Bird
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto de MetaTrader "Angry Bird (Scalping)" usando la API de alto nivel de StockSharp.

## Lógica
- Observa velas de 15 minutos y calcula el máximo más alto y el mínimo más bajo durante las últimas barras `Depth` para derivar un paso de cuadrícula dinámico.
- Cuando no hay posición abierta y la vela anterior cierra por encima de la actual, el RSI en el marco temporal horario dispara entradas: valores por encima de `RsiMin` abren posiciones cortas, valores por debajo de `RsiMax` abren posiciones largas.
- Si existe una posición y el precio se mueve en contra al menos el paso de cuadrícula, se abre una nueva posición en la misma dirección con su volumen multiplicado por `LotExponent` hasta que se alcance `MaxTrades`.
- Una lectura fuerte del CCI por encima de `CciDrop` para cortos o por debajo de `-CciDrop` para largos fuerza el cierre de todas las posiciones.
- Las posiciones también se cierran cuando el beneficio alcanza `TakeProfit` o la pérdida alcanza `StopLoss` relativo al precio de entrada promedio.

## Parámetros
- `StopLoss` – stop loss en puntos.
- `TakeProfit` – take profit en puntos.
- `DefaultPips` – distancia mínima entre órdenes de cuadrícula en pips.
- `Depth` – número de velas usadas para el cálculo del máximo/mínimo.
- `LotExponent` – multiplicador para el volumen de órdenes subsiguientes.
- `MaxTrades` – número máximo de posiciones de promediado.
- `RsiMin` / `RsiMax` – umbrales de RSI para entrada.
- `CciDrop` – valor absoluto del CCI que fuerza el cierre de posiciones.
- `Volume` – volumen inicial de la orden.
- `CandleType` – marco temporal de las velas de trabajo (predeterminado 15 minutos).

## Uso
Adjunte la estrategia a un instrumento y comience. La estrategia usa órdenes de mercado y gestiona una sola posición neta, promediando mientras el precio se mueve en contra.
