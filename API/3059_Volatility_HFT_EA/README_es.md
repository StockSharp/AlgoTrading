# Estrategia Volatility HFT EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto **Volatility HFT EA** de MetaTrader 5 a la API de alto nivel de StockSharp. Reproduce la lógica original que compra cuando el precio de cierre salta muy por encima de una media móvil simple rápida y espera un retroceso hacia esa media. La generación de órdenes, la gestión de indicadores y las salidas protectoras siguen las directrices de `AGENTS.md` mientras mantienen el comportamiento del script MQL.

## Cómo funciona

1. **Configuración del indicador** – se calcula una única media móvil simple (longitud por defecto: 5) en el marco temporal de trabajo especificado por `CandleType`.
2. **Detección de nueva barra** – el procesamiento ocurre solo una vez que una vela está terminada (`CandleStates.Finished`), imitando las verificaciones `IsNewBar` en el EA.
3. **Requisito de calentamiento** – la estrategia espera 60 velas completadas antes de evaluar operaciones, coincidiendo con la guardia `Bars < 60` usada en MQL.
4. **Filtro de entrada** – aparece una configuración larga cuando el último cierre está al menos `MaDifferencePips` por encima de la SMA (diferencia convertida a precio usando el tamaño del pip del instrumento) y el valor de la SMA es más alto que hace dos barras. El EA original usaba `val[0] < -0.0015` y `MA_Val1[0] > MA_Val1[2]`; este port verifica las mismas condiciones sin almacenar manualmente los arrays.
5. **Una posición a la vez** – solo se soportan operaciones largas porque la rama de venta fue comentada en el archivo fuente. Una nueva señal se ignora mientras hay una posición abierta.

## Gestión de riesgo

- **Stop-loss** – stop de protección opcional expresado en pips. El código deriva el tamaño del pip de `Security.PriceStep`, multiplicando por 10 cuando el instrumento tiene 3 o 5 decimales, reproduciendo el escalado `_Digits` de MetaTrader.
- **Take-profit** – el objetivo de salida está anclado al valor de la SMA capturado en la entrada, imitando la llamada `mrequest.tp = MA_Val1[0];`. La estrategia cierra la posición cuando el mínimo de la vela toca el nivel SMA almacenado, emulando una orden limitada en la media.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `OrderVolume` | Volumen usado para cada orden de mercado. |
| `FastMaLength` | Período de la media móvil simple rápida (por defecto 5). |
| `StopLossPips` | Distancia del stop-loss en pips; establecer en `0` para deshabilitar. |
| `MaDifferencePips` | Distancia mínima (en pips) entre el cierre y la SMA requerida para una entrada larga. |
| `CandleType` | Marco temporal usado para la suscripción de velas y cálculos del indicador. |

`MinimumBars` es una constante interna fija igual a `60`, reflejando el requisito del EA para suficiente historial.

## Uso

1. Adjunte la estrategia a un instrumento y seleccione el `CandleType` deseado (por ejemplo, barras de 1 minuto para comportamiento de alta frecuencia).
2. Ajuste `FastMaLength`, `MaDifferencePips` y `StopLossPips` para adaptarse a la volatilidad del instrumento. Las entradas basadas en pips se convierten automáticamente usando el tamaño de pip detectado, por lo que los mismos valores por defecto funcionan en símbolos de FX de 4 y 5 dígitos.
3. Configure `OrderVolume` para que coincida con sus reglas de dimensionamiento del portafolio. La estrategia solo envía órdenes de mercado y no piramidará posiciones.
4. Inicie la estrategia. Se suscribirá a las velas elegidas, construirá la SMA, esperará 60 barras de calentamiento y luego evaluará entradas en cada vela completada.
5. Monitoree la gestión de operaciones: las salidas se desencadenan por el toque de la SMA o por el precio del stop-loss derivado en la entrada.

## Notas y diferencias con el EA original

- La versión MQL solicitaba el tamaño mínimo de lote a través de `SymbolInfoDouble(Symbol(), SYMBOL_VOLUME_MIN)`; aquí el volumen se expone como parámetro para mantener la estrategia flexible entre brokers y clases de activos.
- Las condiciones de venta se omiten porque estaban comentadas en `Volatility_HFT_EA.mq5`. El comportamiento por tanto coincide con el script publicado, que solo ejecutaba la rama larga.
- El manejo del take-profit usa mínimos de velas para detectar un toque de la media móvil en lugar de registrar una orden limitada, asegurando que la misma intención funcione de manera confiable dentro del flujo de trabajo de StockSharp.
- La gestión manual de arrays (`CopyRates`, `CopyBuffer`, `ArraySetAsSeries`) se reemplaza por enlaces de indicadores de StockSharp. Esto reduce el código repetitivo mientras preserva los umbrales originales y las comparaciones de pendiente.
- Todos los cálculos siguen siendo basados en velas; la estrategia no consulta hacia atrás los buffers de indicadores con `GetValue`, manteniéndose en conformidad con las directrices del repositorio.
