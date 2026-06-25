# Estrategia Híbrida Martingale VI (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Martingale VI Hybrid es una conversión del asesor experto original de MetaTrader a la API de alto nivel de StockSharp. Combina un filtro de medias móviles rápida/lenta con una confirmación MACD y escala en posiciones usando un multiplicador de martingala. La estrategia acumula posiciones cuando el precio se mueve en contra de la última entrada una distancia fija en pips y unifica el take profit de todo el clúster al nivel definido por el orden más reciente. Las salidas globales adicionales incluyen beneficio fijo en dinero, beneficio como porcentaje del capital inicial y un trailing stop en dinero.

## Lógica de trading
1. **Filtro de señal** – se utilizan los valores de la vela anterior para las SMA rápida y lenta y el histograma MACD. Un ciclo largo comienza cuando la SMA rápida estaba por encima de la SMA lenta y la línea principal MACD estaba por debajo de su línea de señal. Un ciclo corto comienza cuando la SMA rápida estaba por debajo de la SMA lenta mientras la línea principal MACD estaba por encima de la señal.
2. **Posición inicial** – cuando comienza un nuevo ciclo y no hay posición abierta, la estrategia envía una orden de mercado con el `Initial Volume`.
3. **Adiciones de martingala** – mientras hay una posición abierta, la estrategia monitoriza el último precio de entrada. Si el precio se mueve en contra de la posición `Pip Step` pips, añade otra orden de mercado cuyo volumen es `volumen del orden anterior × Volume Multiplier`. El número de órdenes activas está limitado por `Max Trades`. Cuando se alcanza el límite y `Close Max Orders` está habilitado, toda la posición se cierra inmediatamente.
4. **Take profit compartido** – cada nueva orden actualiza el nivel común de take profit a `precio de entrada ± Take Profit (pips)` según la dirección. Una vez que el máximo de la vela (para largos) o el mínimo (para cortos) toca este nivel, todas las órdenes se cierran juntas.
5. **Salidas globales** – el beneficio flotante se evalúa continuamente:
   - Si `Use Money TP` está habilitado y el beneficio alcanza `Money TP`, la posición se cierra.
   - Si `Use Percent TP` está habilitado y el beneficio alcanza `Percent TP` por ciento del valor inicial del portafolio, la posición se cierra.
   - Si `Enable Trailing` está activo, se aplica un trailing stop en dinero una vez que el beneficio supera `Trailing Activation`. La posición se cierra si el beneficio cae `Trailing Drawdown` desde el máximo.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `Candle Type` | Serie de velas primaria utilizada para las actualizaciones de indicadores.
| `Fast MA`, `Slow MA` | Períodos de las medias móviles simples que definen el filtro de tendencia.
| `MACD Fast`, `MACD Slow`, `MACD Signal` | Parámetros del indicador MACD utilizados para confirmación.
| `Initial Volume` | Volumen de la primera orden en un ciclo de martingala.
| `Volume Multiplier` | Multiplicador aplicado al volumen del orden anterior en cada adición.
| `Max Trades` | Número máximo de órdenes simultáneas en la secuencia de martingala.
| `Take Profit (pips)` | Distancia del take profit para cada orden; la última orden define el precio de take profit compartido.
| `Pip Step` | Movimiento de precio en contra del ciclo actual que activa la siguiente adición.
| `Use Money TP`, `Money TP` | Habilita y establece el objetivo de beneficio en moneda de cuenta.
| `Use Percent TP`, `Percent TP` | Habilita y establece el objetivo de beneficio como porcentaje del valor inicial del portafolio.
| `Enable Trailing`, `Trailing Activation`, `Trailing Drawdown` | Parámetros del trailing stop basado en efectivo que protege el beneficio acumulado.
| `Close Max Orders` | Cuando está habilitado, toda la posición se cierra en cuanto se alcanza el límite de órdenes de martingala.

## Gestión de riesgos
- La estrategia admite objetivos de beneficio tanto absolutos como basados en porcentaje para asegurar ganancias anticipadamente.
- El trailing stop en dinero evita que la posición ceda más que el drawdown configurado después de una racha rentable.
- Limitar el número total de pasos de martingala evita un crecimiento ilimitado de la posición; habilitar `Close Max Orders` fuerza una salida de emergencia cuando la secuencia alcanza su límite configurado.

## Notas de implementación
- La estrategia utiliza la API de alto nivel `SubscribeCandles` de StockSharp con indicadores vinculados mediante `BindEx` para MACD y procesamiento manual para las medias móviles.
- El tamaño del pip se deriva del paso de precio del instrumento, incluido soporte para cotizaciones de 5 y 3 dígitos.
- Los cálculos de beneficio dependen de `Security.PriceStep`, `Security.StepPrice` y `PositionAvgPrice`, asegurando compatibilidad con instrumentos que proporcionan los metadatos necesarios.
