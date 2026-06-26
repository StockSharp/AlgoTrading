# Estrategia de Hedger Drawdown
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port de StockSharp del asesor experto de MetaTrader 5 **hedger.mq5** (MQL #23511). El sistema original abre una cobertura protectora en la dirección opuesta cuando una posición existente cae un número determinado de pips. Una vez que el precio retrocede en una cantidad menor, la cobertura se cierra incluso con pérdida, permitiendo que la operación original se recupere. Esta conversión reproduce el comportamiento con la API de alto nivel de StockSharp y adapta la mecánica al modelo de posición neta de la plataforma.

## Lógica de trading

1. La estrategia monitorea el cierre de cada vela del marco temporal configurado.
2. Para cada posición long que no sea de cobertura, comprueba si la distancia entre el precio de entrada y el cierre actual es mayor o igual que **DrawdownOpenPips**. Si no hay cobertura short activa, abre una con el mismo volumen.
3. Para cada posición short que no sea de cobertura, aplica la regla simétrica, abriendo una cobertura long después de que la pérdida alcanza el umbral de apertura.
4. Las coberturas activas se cierran cuando su pérdida flotante alcanza **DrawdownClosePips**, reflejando la lógica de MetaTrader de liberar la protección después de una recuperación parcial.
5. Cuando la cuenta está plana y **StartWithLong** está habilitado, el algoritmo abre una posición long inicial para iniciar el ciclo.

Dado que StockSharp rastrea posiciones netas, la estrategia mantiene registros internos de entradas long y short (incluidas las que son coberturas). Cada orden de mercado actualiza los registros para que las coberturas puedan abrirse y cerrarse independientemente, incluso si el broker colapsa las posiciones.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `DrawdownOpenPips` | Drawdown en pips que activa la apertura de la cobertura opuesta. |
| `DrawdownClosePips` | Drawdown en pips que fuerza el cierre de la cobertura. |
| `InitialVolume` | Volumen de la operación inicial al iniciar el ciclo. |
| `StartWithLong` | Si está habilitado, abre la posición long inicial cuando la cuenta está plana. |
| `EnableVerboseLogging` | Escribe las acciones de cobertura en el registro de la estrategia para depuración. |
| `CandleType` | Serie de velas utilizada para monitorear los drawdowns. |

## Diferencias con la versión de MetaTrader

- El asesor experto dependía de comentarios en los tickets (`hedge_buy` / `hedge_sell`) para distinguir las posiciones de cobertura. La conversión almacena este estado en memoria porque StockSharp usa netting.
- Las verificaciones de margen y configuraciones de slippage se omiten; la colocación de órdenes usa los helpers de alto nivel `BuyMarket` / `SellMarket`.
- La estrategia expone rangos de optimización para los umbrales de pips y el volumen para que puedan ajustarse con los optimizadores de StockSharp.

## Notas de uso

1. Adjunte la estrategia al símbolo y portfolio deseados.
2. Ajuste los umbrales de pips para que coincidan con la volatilidad del instrumento.
3. Habilite el registro detallado al validar la conversión: el registro registra cada creación y eliminación de cobertura con estadísticas de pips.
4. Implemente en marcos temporales que entreguen cierres de velas significativos (por ejemplo, M15 a H1) para evitar el sobretrading.
