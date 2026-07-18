# Estrategia FineTuning MA Candle Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Puerto en C# del asesor experto de MetaTrader 5 **Exp_FineTuningMACandle_Duplex**.
- Replica el indicador de vela FineTuningMA en dos flujos independientes para poder ajustar la lógica larga y corta por separado.
- Diseñado para la API de estrategia de alto nivel de StockSharp: las suscripciones, indicadores, gestión del riesgo y dibujo de gráficos se gestionan automáticamente por el framework.

## Modelo de vela FineTuningMA
- El indicador original construye una vela sintética aplicando tres exponentes ponderados (`Rank1`–`Rank3`) y coeficientes de desplazamiento correspondientes a las últimas `Length` barras.
- Los valores ponderados resultantes de apertura y cierre se comparan para generar un código de color: `2` para alcista, `1` para neutral, `0` para bajista.
- Cuando el cuerpo real de la vela es menor que el `Gap` configurable, la apertura sintética se iguala al cierre sintético anterior. Esto reproduce la lógica de "cuerpo plano" de la versión MQL5.
- El indicador en este puerto emite solo el flujo de color (valores decimales 0/1/2) porque las reglas de trading dependen exclusivamente de las transiciones de color.

## Lógica de trading
1. Se suscribe a dos feeds de velas (`LongCandleType` y `ShortCandleType`). Pueden apuntar al mismo marco temporal o a diferentes.
2. Para cada feed se crea una instancia dedicada del indicador FineTuningMA con sus propios parámetros de ponderación y desplazamiento de señal (`SignalBar`).
3. Los eventos de vela completada se procesan con las siguientes reglas:
   - **Salida larga** – si el color anterior es igual a `0`, la posición larga existente se cierra.
   - **Entrada larga** – si el color anterior es igual a `2` y el color actual cambió desde `2`, se envía una orden de compra (tras cubrir cualquier posición corta).
   - **Salida corta** – si el color anterior es igual a `2`, la posición corta existente se cubre.
   - **Entrada corta** – si el color anterior es igual a `0` y el color actual cambió desde `0`, se envía una orden de venta (tras cubrir cualquier posición larga).
4. El volumen de la orden está controlado por `OrderVolume`. Cuando se requiere un reversal, la estrategia añade automáticamente la posición actual absoluta para que la posición se dé la vuelta en una sola orden de mercado.
5. Las barreras de protección opcionales (`TakeProfitPoints`, `StopLossPoints`) se traducen en puntos de precio y se aplican a través de `StartProtection`.

## Parámetros
### Flujo largo
- `LongCandleType` – tipo de datos de vela (marco temporal) para el flujo del indicador largo.
- `LongLength` – número de barras usadas en el cálculo ponderado.
- `LongRank1`, `LongRank2`, `LongRank3` – coeficientes exponentes que dan forma a la curva de peso a través de la ventana.
- `LongShift1`, `LongShift2`, `LongShift3` – modificadores adicionales (0…1) que sesgan los pesos hacia el inicio o el final de la ventana.
- `LongGap` – tamaño máximo del cuerpo real de la vela que mantiene el precio sintético de apertura igual al cierre sintético anterior.
- `LongSignalBar` – cuántas velas completadas omitir antes de leer la señal (`0` evalúa la última vela cerrada, `1` usa la anterior, etc.).
- `EnableLongEntries` – activa las entradas largas.
- `EnableLongExits` – activa las salidas largas automáticas.

### Flujo corto
- `ShortCandleType` – tipo de datos de vela para el flujo del indicador corto.
- `ShortLength`, `ShortRank1`, `ShortRank2`, `ShortRank3`, `ShortShift1`, `ShortShift2`, `ShortShift3`, `ShortGap`, `ShortSignalBar` – idénticos a sus contrapartes del lado largo pero aplicados al flujo corto.
- `EnableShortEntries` – activa las entradas cortas.
- `EnableShortExits` – activa las salidas cortas automáticas.

### Trading
- `OrderVolume` – cantidad base para nuevas posiciones. Los reversals añaden automáticamente la posición actual absoluta a este valor.
- `TakeProfitPoints` – distancia opcional de take-profit expresada en puntos de precio (0 lo deshabilita).
- `StopLossPoints` – distancia opcional de stop-loss expresada en puntos de precio (0 lo deshabilita).

## Notas
- El asesor experto original incluía modos de gestión del dinero basados en saldo o margen. El puerto expone un parámetro `OrderVolume` fijo más sencillo. Ajústelo para que coincida con el tamaño de posición deseado.
- `StartProtection` se invoca solo cuando el instrumento expone un paso de precio válido (`Security.Step > 0`).
- No se proporciona ninguna versión Python intencionalmente.
- Las áreas de gráfico se crean automáticamente: si los feeds de velas largas y cortas difieren, se muestran dos paneles separados; de lo contrario solo se muestra uno.
- La estrategia depende de velas completadas; no reacciona a actualizaciones intrabarra.
