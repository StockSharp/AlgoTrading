# Estrategia Experto MACD Largo/Corto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Experto MACD Largo/Corto** es una conversión de StockSharp del experto de MetaTrader "LongShortExpertMACD". Combina la lógica de cruce estándar de la Convergencia/Divergencia de Medias Móviles (MACD) con controles de riesgo de distancia fija. La estrategia reacciona a los cruces entre la línea MACD y su línea de señal, puede operar en modos de solo largo, solo corto o bidireccional, y aplica automáticamente niveles de take-profit y stop-loss expresados en puntos de precio.

La implementación utiliza el API de alto nivel de StockSharp con suscripciones de velas y vinculaciones de indicadores. Las órdenes se registran como órdenes de mercado, lo que hace que la estrategia sea sencilla de conectar tanto a fuentes de datos en tiempo real como históricas.

## Indicadores y Datos de Mercado
- **Velas** – un único marco temporal proporcionado por el parámetro `CandleType` (marco temporal de 1 minuto por defecto). La estrategia se suscribe a esta serie de velas mediante `SubscribeCandles`.
- **MovingAverageConvergenceDivergenceSignal** – el indicador MACD de StockSharp con longitudes configurables de EMA rápida, EMA lenta y EMA de señal. El valor del histograma se deriva implícitamente de la diferencia entre las salidas del MACD y la señal.

## Lógica de Trading
1. **Preparación de señal**
   - En cada vela finalizada los valores de MACD y señal se recuperan a través de la vinculación del indicador.
   - El estado histórico `_prevIsMacdAboveSignal` rastrea si el MACD estaba por encima de la línea de señal durante la vela anterior.

2. **Criterios de entrada**
   - **Cruce alcista**: cuando el MACD cruza por encima de la línea de señal, la estrategia abre una posición larga si la dirección de trading configurada permite entradas largas.
     - Si ya hay una posición corta activa y el modo de reversión está habilitado (`AllowedPosition = Both`), el tamaño de la orden incluye el volumen corto actual para cerrar la posición y pasar a largo en una única orden de mercado.
     - En modo de solo largo, una posición corta existente se cierra inmediatamente, pero no se abre un nuevo trade largo hasta la siguiente señal.
   - **Cruce bajista**: la acción simétrica para entradas cortas.

3. **Criterios de salida**
   - **Gestión de riesgo**: tanto los niveles de stop-loss como de take-profit se recalculan desde el precio de entrada promedio actual cada vez que se detecta una posición. Las distancias se establecen en puntos de precio (es decir, `Security.PriceStep * parámetro`), lo que mantiene el comportamiento consistente entre instrumentos.
     - Las posiciones largas salen cuando el mínimo de la vela alcanza el nivel de stop-loss o el máximo alcanza el nivel de take-profit.
     - Las posiciones cortas salen cuando el máximo de la vela alcanza el nivel de stop-loss o el mínimo toca el nivel de take-profit.
   - **Cruce opuesto**: si la dirección de trading permite el lado opuesto, la posición se aplana (y opcionalmente se revierte) cuando la relación del indicador cambia.

4. **Salvaguardas operativas**
   - La lógica de trading se ejecuta solo cuando la estrategia está formada, en línea y se permite el trading (`IsFormedAndOnlineAndAllowTrading`).
   - Los niveles de protección se reinician cuando no se tiene ninguna posición para evitar umbrales obsoletos.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `AllowedPosition` | `Both` | Restringe la estrategia a trading solo largo, solo corto o bidireccional. |
| `FastLength` | `12` | Período del EMA rápido dentro del cálculo del MACD. |
| `SlowLength` | `24` | Período del EMA lento dentro del cálculo del MACD. |
| `SignalLength` | `9` | Período del EMA de señal utilizado para la detección de cruces. |
| `TakeProfitPoints` | `50` | Distancia al nivel de take-profit medida en puntos de precio (`PriceStep * puntos`). Establecer en `0` para desactivar. |
| `StopLossPoints` | `20` | Distancia al nivel de stop-loss medida en puntos de precio. Establecer en `0` para desactivar. |
| `CandleType` | `TimeFrame(1 minute)` | Serie de velas utilizada para la generación de señales. |
| `Volume` | `1` | Número de lotes/contratos enviados con cada orden de mercado. |

Todos los parámetros numéricos exponen rangos de optimización para simplificar las pruebas walk-forward dentro de StockSharp Designer o Runner.

## Gestión de Posiciones
- **Lógica de reversión**: cuando se permite el trading bidireccional, la estrategia usa tamaños de orden combinados para invertir posiciones en una única orden de mercado, imitando el comportamiento del experto original de MetaTrader.
- **Modos solo largo / solo corto**: las posiciones existentes en el lado no permitido se cierran inmediatamente, pero no se establece ninguna nueva exposición hasta que ocurra una señal alineada con la dirección permitida.
- **Recálculo de stop/take**: la estrategia recalcula los niveles de protección en cada vela usando el último `PositionAvgPrice`, garantizando distancias correctas incluso después de llenados parciales o entradas escalonadas.

## Notas de Uso
- Asegúrese de que el instrumento proporciona un `PriceStep` válido; si el valor falta, la estrategia retrocede a `1.0` unidades de precio, lo cual es apropiado para instrumentos de tipo acción pero puede requerir ajuste para símbolos de Forex.
- La estrategia depende de velas completadas. Los escenarios sensibles a la latencia deben suministrar velas de granularidad apropiada para evitar retrasos.
- Debido a que las órdenes son de mercado sin controles de deslizamiento, la gestión de riesgo debe considerar posibles diferencias de llenado, especialmente en activos ilíquidos.
- La visualización se crea automáticamente cuando la aplicación anfitriona admite áreas de gráfico; MACD, velas y trades propios se dibujan para monitoreo rápido.

## Notas de Conversión
- La implementación de StockSharp preserva los parámetros configurables de MACD, las distancias de take-profit y stop-loss, y el interruptor de disponibilidad de posición del experto MQL5.
- Los módulos de trailing-stop y gestión de dinero utilizados en MetaTrader se omiten intencionalmente porque su comportamiento es equivalente a las variantes "ninguno" incluidas con el experto original.
