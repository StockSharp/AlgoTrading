# Estrategia Rnd Trade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Conversión del asesor experto de MetaTrader 5 `RndTrade.mq5` a la API de estrategia de alto nivel de StockSharp.
- Cierra cualquier posición existente en un intervalo de tiempo fijo y abre inmediatamente una nueva posición de mercado en una dirección seleccionada aleatoriamente.
- Usa suscripciones de velas basadas en tiempo como reemplazo determinista de las callbacks de temporizador originales.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| ---- | ---- | ------- | ----------- |
| `IntervalMinutes` | `int` | `60` | Número de minutos entre el cierre de la posición actual y la apertura de una nueva posición aleatoria. Debe ser mayor que cero. |
| `Volume` | `decimal` | `1` | Tamaño de posición usado para entradas a mercado. Derivado de la clase base `Strategy`. |

## Suscripciones de datos
- Se suscribe a velas de marco temporal cuya longitud coincide con `IntervalMinutes` (p. ej., `60` → velas de 60 minutos).
- El evento de cierre de vela (`CandleStates.Finished`) se usa para activar la lógica exactamente una vez por intervalo.

## Lógica de trading
1. Esperar la finalización de cada vela de intervalo.
2. Omitir el procesamiento hasta que la estrategia esté formada, en línea y se permita el trading.
3. Cerrar cualquier posición abierta creada durante el intervalo anterior.
4. Generar un valor aleatorio para decidir entre una entrada larga o corta.
5. Enviar una orden de mercado (`BuyMarket` o `SellMarket`) con el volumen configurado en la dirección seleccionada.

## Notas de implementación
- Se basa en `SubscribeCandles().Bind(ProcessCandle)` para evitar el sondeo manual de valores de indicadores o colecciones.
- Llama a `StartProtection()` durante el inicio para que el módulo de riesgo integrado esté activo, aunque no se configure ningún stop-loss o take-profit explícito.
- Usa `Random` de la biblioteca estándar para imitar el comportamiento `MathRand()` encontrado en la estrategia MQL original.
- El código contiene comentarios en inglés que explican cómo cada paso de conversión se mapea a las características de StockSharp.

## Diferencias con la estrategia MQL original
- Los eventos de temporizador (`OnTimer`) se emulan mediante suscripciones de velas en lugar de la API de temporizador de MetaTrader.
- El cierre de posición se maneja con `ClosePosition()` en lugar de iterar sobre listas de posiciones y llamar `PositionClose` para cada ticket.
- La versión de StockSharp se basa en la propiedad integrada `Volume` para el dimensionamiento de posiciones en lugar de la consulta del lote mínimo del símbolo.
- Las reglas de llenado de órdenes y la configuración de deslizamiento son gestionadas por el bróker o simulador conectado, por lo que no se configuran explícitamente en la estrategia.

## Uso
1. Adjuntar la estrategia a una cartera y un instrumento dentro del entorno StockSharp.
2. Configurar `IntervalMinutes` y `Volume` según la frecuencia de trading y el tamaño deseados.
3. Iniciar la estrategia. Aplanará y reabrirá posiciones automáticamente en cada intervalo sin ninguna entrada adicional.
4. No se proporciona implementación en Python en este momento; solo está disponible la versión en C#.
