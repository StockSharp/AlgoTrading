# Diagrama de sesión de límites con RSI y Momentum
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama combina RSI(14), Momentum(14), un filtro Working time de día completo y límites pendientes gestionados. Sobreventa con impulso débil coloca una compra bajo la apertura; sobrecompra con impulso fuerte coloca una venta encima, y la señal caducada cancela expresamente su orden.

![schema](schema.svg)

## Resumen de la estrategia

- Velas terminadas de cinco minutos alimentan RSI, Momentum, la apertura para entradas y el cierre para evaluar la protección.
- Working time acepta velas de 00:00 a 23:59, como la sesión práctica de día completo del código, pero mantiene visible el bloque configurable.
- RSI bajo 30, Momentum bajo 1 y Position <= 0 habilitan compra; RSI sobre 70, Momentum sobre 1 y Position >= 0 habilitan venta.
- Banderas de un solo disparo mantienen como máximo una orden por episodio y se cancelan órdenes propias caducadas o contrarias.
- Un límite ejecutado recibe beneficio absoluto 35 y pérdida 8, con el cierre conectado al precio de la protección.

## Reglas de entrada y salida

- **Entrada en largo**: En sesión, RSI < 30, Momentum < 1 y Position <= 0 registran una compra de una unidad en OpenPrice − 25, cancelando antes cualquier venta activa.
- **Entrada en corto**: En sesión, RSI > 70, Momentum > 1 y Position >= 0 registran una venta de una unidad en OpenPrice + 25, cancelando antes cualquier compra activa.
- **Salida**: La protección cierra con ganancia absoluta 35 o pérdida 8. La compra pendiente se cancela si deja de cumplirse RSI, Momentum o posición; la venta es simétrica.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candle Time Frame | 00:05:00 | Intervalo terminado; cinco minutos es la adaptación replay y C# usa quince por defecto. |
| RSI Period | 14 | Cantidad de valores de RelativeStrengthIndex. |
| Momentum Period | 14 | Cantidad de valores de Momentum. |
| Session Start | 00:00:00 | Inicio de la ventana Working time. |
| Session End | 23:59:00 | Fin de Working time; 23:59 conserva el comportamiento diario completo. |
| RSI Buy Threshold | 30 | RSI debe estar debajo para comprar. |
| RSI Sell Threshold | 70 | RSI debe estar encima para vender. |
| Momentum Threshold | 1 | Momentum debe estar debajo para comprar y encima para vender. |
| Limit Offset, price units | 25 | Distancia absoluta restada o sumada a OpenPrice en replay. |
| Order Volume | 1 | Volumen de cada límite pendiente. |
| Take Profit, price units | 35 | Distancia favorable absoluta desde la ejecución. |
| Stop Loss, price units | 8 | Distancia adversa absoluta desde la ejecución. |

## Detalles del diagrama

- C# usa por defecto velas de 15 minutos. El diagrama usa cinco minutos en replay para mostrar suficientes ciclos; ambos periodos siguen en 14, por lo que es una adaptación explícita de muestreo.
- El código desplaza 5 × PriceStep. Como el diagrama no consume ese paso, usa 25 unidades absolutas; es un ajuste de ejecución de la galería, no el valor fuente.
- Los límites se calculan desde OpenPrice como en ProcessCandle. ClosePrice queda separado y solo actualiza el precio de Position protection.
- Las distancias fuente son 35 × PriceStep y 8 × PriceStep. El diagrama conserva 35 y 8 como unidades absolutas, no como porcentajes.
- Las banderas modelan la comprobación de orden activa y se reinician al invalidarse RSI, Momentum o la condición de posición.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
