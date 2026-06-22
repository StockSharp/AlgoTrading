# Estrategia de Reversión Estrella Vespertina
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port directo del Asesor Experto **EveningStar.mq5** (MQL5 id 18507). Vigila la formación clásica de velas de la Estrella Vespertina y abre una posición tan pronto como la siguiente barra comienza a cotizar. La lógica ha sido reescrita sobre la API de alto nivel de StockSharp mientras se mantienen los filtros de patrón y la gestión de riesgo originales.

## Lógica de trading
1. La estrategia se suscribe al marco temporal seleccionado por el parámetro `CandleType`. Todo el procesamiento ocurre únicamente en velas terminadas.
2. Cada vez que se cierra una nueva vela, las últimas instantáneas son almacenadas en caché para que la ventana de tres velas definida por `Shift` pueda ser evaluada.
3. El patrón de la Estrella Vespertina se considera válido cuando:
   - La vela *N-2* (la más antigua) es alcista (`open < close`).
   - La vela *N-1* (la del medio) satisface la preferencia `Candle2Bullish` (alcista por defecto).
   - La vela *N* (la más reciente) es bajista (`open > close`).
   - Si `CheckCandleSizes` está habilitado, la vela del medio debe tener el cuerpo más pequeño de las tres.
   - Si `ConsiderGap` está habilitado, debe haber un gap entre los cuerpos de las velas de la misma manera que en el robot original (el tamaño del gap es igual a un pip calculado a partir del paso de precio del instrumento).
4. Una vez confirmado el patrón, la estrategia verifica la dirección seleccionada por `Direction`:
   - `Short` (predeterminado) abre una orden de venta, coincidiendo con el comportamiento original de la Estrella Vespertina.
   - `Long` permite correr la exposición exactamente opuesta (mantenido para paridad de características con la versión MQL).
5. Antes de abrir una posición, el algoritmo opcionalmente cierra la exposición opuesta si `CloseOppositePositions` está configurado como `true`.
6. Los precios de stop-loss y take-profit se calculan a partir de las distancias en pips (`StopLossPips`, `TakeProfitPips`) usando el mismo ajuste de 3/5 dígitos que existía en MetaTrader.
7. El tamaño de la posición se deriva del valor actual de la cartera y `RiskPercent`. Si el volumen calculado es menor que el tamaño mínimo negociable, la señal es ignorada.

## Gestión de posiciones
- Cuando una posición larga está activa, la estrategia monitorea cada nueva vela. Si el precio mínimo cae por debajo del nivel del stop o el precio máximo alcanza el nivel de take-profit, toda la posición se cierra a mercado.
- Cuando una posición corta está activa, se aplica la misma lógica con comparaciones invertidas.
- Si el valor de la cartera o la distancia al stop son iguales a cero, el tamaño de la orden no puede calcularse, por lo tanto la entrada es omitida.

## Parámetros
| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `Direction` | `Short` | Elige si el patrón debe abrir una posición larga o corta. |
| `TakeProfitPips` | `150` | Distancia al objetivo de beneficio expresada en pips. Establecer en cero para deshabilitar. |
| `StopLossPips` | `50` | Distancia al stop de protección en pips. Un valor no positivo deshabilita la operación. |
| `RiskPercent` | `5` | Porcentaje del capital de la cartera arriesgado por operación. Usado para calcular el volumen de la orden. |
| `Shift` | `1` | Número de barras omitidas desde la vela más reciente antes de evaluar el patrón. |
| `ConsiderGap` | `true` | Requiere un gap entre cuerpos de velas igual que el Asesor Experto original. |
| `Candle2Bullish` | `true` | Obliga a que la vela del medio sea alcista. Deshabilitar para requerir una vela del medio bajista. |
| `CheckCandleSizes` | `true` | Asegura que la vela del medio tenga el cuerpo absoluto más pequeño. |
| `CloseOppositePositions` | `true` | Cierra la exposición opuesta antes de enviar la nueva orden. |
| `CandleType` | Marco temporal `1H` | Series de velas usadas para análisis. |

## Notas
- El tamaño del pip se deriva del paso de precio del instrumento. Para símbolos forex de 3 y 5 dígitos, un pip equivale a diez pasos de precio, reproduciendo el comportamiento del EA original.
- Si `StopLossPips` es cero, el tamaño de la posición no puede calcularse y la señal es ignorada para prevenir riesgo ilimitado.
- La estrategia recorta automáticamente el historial en caché, por lo que el uso de memoria permanece constante incluso en sesiones largas.
