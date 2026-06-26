# Estrategia de Tengri (Port de StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una recreación de alto nivel en StockSharp del asesor experto de MetaTrader *Tengri*. El asesor original opera EURUSD y USDCHF con un enfoque de grid y escalado impulsado por RSI, filtros de volatilidad "Silence" personalizados y un indicador de tendencia EMA. La versión en C# mantiene el núcleo de comportamiento mientras lo adapta a las convenciones de StockSharp y la contabilidad de posición neta.

## Ideas Principales

- **Sesgo direccional** – compara el bid actual con el precio de apertura de una vela de marco temporal superior (30 minutos por defecto). Una diferencia positiva sesga la estrategia hacia largo, una diferencia negativa hacia corto.
- **Filtro de momentum** – un RSI de 14 períodos calculado en velas horarias debe mantenerse por debajo de 70 para entradas largas y por encima de 30 para entradas cortas, coincidiendo con la lógica de MetaTrader.
- **Filtros de mercado tranquilo** – el indicador personalizado "Silence" original se emula con valores ATR suavizados por EMAs en dos marcos temporales diferentes. Ambos filtros deben mantenerse por debajo de umbrales configurables para permitir entradas o ampliaciones.
- **Confirmación de tendencia** – una EMA en un marco temporal medio asegura que las adiciones largas ocurran solo por encima de la EMA y las adiciones cortas solo por debajo.
- **Sizing de grid y martingala** – el primer trade usa un lote fijo o proporcional al patrimonio. Los trades adicionales multiplican el volumen anterior por factores configurables (1.70 antes de `StepX`, 2.08 después por defecto).
- **Espaciado de pips** – la distancia entre órdenes de grid sigue dos pasos base (10 pips y 20 pips por defecto) y puede crecer exponencialmente a través de `PipStepExponent`.

## Flujo de Trading

1. **Evaluación de entrada** (por `EntryCandleType`, por defecto M1):
   - Determinar la dirección desde la vela `DealCandleType`.
   - Verificar RSI y el primer filtro de silencio.
   - Asegurar que no haya trades activos en la misma dirección (las posiciones en dirección opuesta se aplanan primero porque las carteras de StockSharp son netas).
   - Enviar una orden de mercado con el tamaño de lote calculado. El primer trade almacena un objetivo de take-profit basado en pips.
2. **Evaluación de ampliación** (por `ScaleCandleType`, por defecto M1):
   - Confirmar la tendencia EMA y el segundo filtro de silencio.
   - Verificar que el último precio de ejecución esté suficientemente lejos del mercado actual usando las reglas de pip-step.
   - Añadir otra orden de mercado con sizing martingala mientras la dirección permanezca válida y el conteo de trades esté por debajo de `MaxTrades`.
3. **Gestión de posición**:
   - El objetivo de ganancia global opcional cierra la posición cuando existen pilas largas y cortas y el PnL no realizado combinado supera `Equity / LimitDivisor`.
   - El take-profit del primer trade actúa como salida simple: cuando el bid/ask alcanza el objetivo almacenado, toda la posición neta se aplana.
   - No se usa stop-loss automático, espejando el código MetaTrader.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `DealCandleType` | Marco temporal cuyo precio de apertura define el sesgo direccional. |
| `EntryCandleType` | Marco temporal para evaluar señales de entrada. |
| `ScaleCandleType` | Marco temporal para verificar adiciones de grid. |
| `MaCandleType` | Marco temporal para el filtro de tendencia EMA. |
| `Silence1CandleType` / `Silence2CandleType` | Marcos temporales para filtros de volatilidad basados en ATR. |
| `RsiPeriod` | Longitud del RSI (por defecto 14). |
| `SilencePeriod1/2`, `SilenceInterpolation1/2`, `SilenceLevel1/2` | Suavizado ATR y umbrales que controlan los dos filtros de silencio. |
| `MaPeriod` | Período de la EMA. |
| `PipStep`, `PipStep2`, `PipStepExponent` | Distancias entre trades de ampliación. |
| `LotExponent1`, `LotExponent2`, `StepX` | Factores martingala para posiciones adicionales. |
| `LotSize`, `FixLot`, `LotStep` | Configuración de gestión monetaria para la primera posición. |
| `SlTpPips` | Distancia en pips para fijar un take-profit para el primer trade (0 lo deshabilita). |
| `MaxTrades` | Número máximo de entradas por dirección. |
| `UseLimit`, `LimitDivisor` | Configuración de bloqueo de ganancia global. |
| `CloseFriday`, `CloseFridayHour` | Bloqueo opcional de entrada tardía el viernes. |

## Diferencias con la Versión MetaTrader

- **Reemplazo del indicador Silence** – el indicador "Silence" propietario se aproxima con valores ATR suavizados por EMAs. Los umbrales mantienen la misma escala numérica pero pueden ajustarse si el proxy ATR se comporta diferente.
- **Contabilidad de posición neta** – las carteras de StockSharp son netas, por lo que la estrategia aplana la dirección opuesta antes de abrir una nueva pila en lugar de cubrir ambos lados simultáneamente.
- **Manejo de take-profit** – MetaTrader adjunta TP solo a la primera orden. El port cierra la posición neta completa cuando se activa ese objetivo. Las órdenes adicionales intencionalmente no tienen TP, coincidiendo con el modelo de riesgo original.
- **Elección de símbolo** – la estrategia usa el `Security` asignado a la instancia de estrategia. Configurar instancias separadas para EURUSD, USDCHF u otro instrumento.

## Notas de Uso

- Configurar el paso de volumen y los volúmenes mín/máx en el instrumento objetivo para que el redondeo estilo `LotCheck` se alinee con los requisitos del broker.
- La estrategia asume que las cotizaciones del broker proporcionan actualizaciones del mejor bid/ask. Sin datos de Nivel 1 los controles de dirección y TP no pueden operar.
- Como no hay stop-loss, considerar ejecutar la estrategia con controles de riesgo externos (stop de patrimonio, supervisión manual, etc.).

## Visualización

Para analizar el comportamiento conectar widgets de gráfico a las series de velas suscritas (marcos temporales de dirección, entrada y escalado) más superponer los indicadores EMA y ATR. Esto refleja las herramientas de diagnóstico utilizadas con el asesor original.
