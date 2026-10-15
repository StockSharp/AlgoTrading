# Cesta ponderada de señales con límites caducables
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama combina un voto de zona RSI y otro de posición frente a EMA en una puntuación de −3 a +3. Un cruce estando plano registra un límite al cierre terminado; Combination<Order> entrega esa orden a N values por doce velas y a Order cancellation, y la ejecución inicia Position protection de 1,2%/0,8%.

![schema](schema.svg)

## Resumen de la estrategia

- RSI bajo 30 aporta +2, sobre 70 aporta −2 y la zona media aporta cero.
- Close sobre EMA(20) aporta +1 y por debajo −1; Formula suma ambos votos ponderados.
- Las comparaciones actual y Previous value detectan un cruce nuevo de +1 para largo o −1 para corto, siempre con Position == 0.
- Compra y venta comparten el close terminado y volumen uno; sus Order convergen en Combination y sus MyTrade alimentan Position protection.
- N values cuenta doce velas terminadas desde el registro y Order cancellation retira la orden actual sin ejecutar.

## Reglas de entrada y salida

- **Entrada en largo**: La puntuación pasa de menos de +1 a por lo menos +1 con posición plana. Order registering coloca una compra limitada al close terminado.
- **Entrada en corto**: La puntuación pasa de más de −1 a como máximo −1 con posición plana. Order registering coloca una venta limitada al close terminado.
- **Salida**: Una entrada ejecutada queda protegida a +1,2% y −0,8% del fill. Un límite pendiente llega como objeto Order a Order cancellation tras doce velas contadas por N values.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo terminado; cinco minutos adaptan el valor C# de 60 minutos al replay mensual. |
| RSI Length | 14 | Periodo RSI; C# usa 21 por defecto y el diagrama compacto 14. |
| EMA Length | 20 | Periodo EMA; C# usa 50 por defecto y el diagrama 20. |
| RSI Weight | 2 | Peso del voto RSI sobrevendido o sobrecomprado. |
| Trend Weight | 1 | Peso del voto del close respecto de EMA. |
| Replay Signal Threshold | 1 | Frontera replay; el valor 2 del blueprint queda como alternativa documentada. |
| Cancel After N Candles | 12 | Velas terminadas antes de intentar cancelar una entrada pendiente. |
| Take Profit, % | 1.2 | Ganancia porcentual de Position protection. |
| Stop Loss, % | 0.8 | Pérdida porcentual de Position protection. |
| Order Volume | 1 | Volumen de cada entrada limitada. |

## Detalles del diagrama

- El C# ejecutable usa por defecto 60 minutos, RSI(21), EMA(50), comportamiento de umbral 2 y cooldown de cuatro velas; también puntúa dirección de vela y zonas RSI intermedias. Este diagrama usa 5 minutos, 14/20, dos votos y sin cooldown separado.
- El blueprint revisado proponía umbral 2. Con solo dos votos, el replay de marzo no creó órdenes porque RSI sobrevendido solía coincidir con precio bajo EMA y ambos votos se anulaban. Por eso el valor replay transparente es 1; sigue expuesto para restaurar 2.
- El README vecino describe ocho patrones, desplazamiento pendiente, expiración y protección del experto original. El C# actual implementa tres familias de puntuación y entradas a mercado, sin bloques de expiración ni protección.
- El límite al close reemplaza deliberadamente la entrada a mercado para dar un ciclo real a Combination, N values y Order cancellation. Se desactiva el ajuste de precio porque el instrumento replay no aporta paso.
- A diferencia de Position <= 0 / >= 0 en C#, el diagrama solo entra plano y no revierte. La protección porcentual 1,2/0,8 es educativa, no lógica del C# ejecutable.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
