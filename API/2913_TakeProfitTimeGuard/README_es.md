# TakeProfitTimeGuardStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`TakeProfitTimeGuardStrategy` emula el comportamiento del experto de MetaTrader `Exp_GTakeProfit_Tm` supervisando el beneficio a nivel de cuenta y forzando un estado de posición plana fuera de un horario de trading configurable. La estrategia no abre posiciones por sí sola. En cambio, sirve como capa de gestión de riesgo superpuesta que cierra automáticamente cualquier exposición existente una vez que se alcanza el objetivo de beneficio o cuando el trading debe detenerse fuera del rango de tiempo permitido.

## Lógica principal

- Se suscribe a un flujo de velas configurable (por defecto 1 minuto) para evaluar el PnL realizado y no realizado usando el precio de cierre más reciente.
- Calcula el **beneficio total** como la suma del PnL realizado (`Strategy.PnL`) y el PnL flotante derivado del precio promedio de posición actual.
- Ignora las pérdidas mientras la ventana de trading está abierta, replicando el comportamiento original del asesor experto.
- Una vez que se alcanza el **objetivo de take-profit**, establece un indicador de stop interno y liquida repetidamente cualquier posición restante hasta que la cuenta esté plana. El indicador de stop se reinicia después de que el portafolio vuelva a posición cero.
- Cuando la **ventana de trading** opcional está habilitada, la estrategia cierra todas las posiciones siempre que el tiempo actual cae fuera del rango permitido, también esperando hasta que el libro esté plano antes de rehabilitar el trading.

## Parámetros

| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|----------------|-------------|
| `CandleType` | `DataType` | marco temporal de 1 minuto | Serie de velas usada para evaluar la lógica de beneficio y horario. |
| `TargetMode` | `ProfitTargetModes` (`Percent`/`Currency`) | `Percent` | Selecciona si `TakeProfitValue` se interpreta como porcentaje del capital de la cuenta o como importe absoluto en moneda. |
| `TakeProfitValue` | `decimal` | `100` | Umbral del objetivo de beneficio. Se interpreta según `TargetMode`. Debe ser mayor que cero. |
| `UseTradingWindow` | `bool` | `true` | Habilita o deshabilita el filtro de tiempo. |
| `StartTime` | `TimeSpan` | `00:00:00` | Inicio de la ventana de trading permitida (inclusive). |
| `EndTime` | `TimeSpan` | `23:59:00` | Fin de la ventana de trading permitida. Cuando el tiempo de inicio es mayor que el tiempo de fin, la ventana abarca la medianoche. |

## Notas de comportamiento

1. El valor inicial del portafolio se captura cuando la estrategia inicia (o en la primera actualización si el valor era cero) y se usa como referencia para el objetivo porcentual.
2. La estrategia calcula el PnL flotante usando el precio de cierre del último eje de vela; los resultados dependen de la granularidad de la vela seleccionada.
3. Si se cumple el objetivo de beneficio, la estrategia sigue enviando órdenes de mercado para aplanar la posición hasta que el libro esté vacío. Registra la razón del cierre del libro.
4. Cuando `UseTradingWindow` está habilitado y el reloj está fuera de la ventana, se ejecuta la misma rutina de aplanamiento incluso si el objetivo de beneficio no se alcanzó.
5. El indicador de stop (`_stop`) se borra solo después de que la posición vuelva a cero, permitiendo que el trading se reanude cuando las condiciones lo permitan.

## Diferencias con la estrategia MQL original

- Usa la API de alto nivel de StockSharp (`SubscribeCandles`) en lugar de manejadores por tick.
- Calcula el beneficio flotante desde el precio promedio de posición expuesto por `Strategy.PositionPrice`.
- Registra eventos de take-profit para un monitoreo más fácil.
- La comparación de tiempo se basa en `DateTimeOffset.CloseTime` de las velas suscritas.

## Consejos de uso

- Adjunte la estrategia a un portafolio que ya ejecuta otra estrategia de trading para actuar como capa de guardia.
- Elija un marco temporal de velas que coincida con la capacidad de respuesta requerida para la evaluación de beneficios (por ejemplo, 1 minuto para control rápido).
- Asegúrese de que la información del portafolio (especialmente `CurrentValue`) esté disponible; de lo contrario, establezca un saldo inicial explícito antes de ejecutar objetivos porcentuales.
- La estrategia se puede combinar con `StartProtection()` en otra estrategia primaria para agregar controles de riesgo adicionales.
