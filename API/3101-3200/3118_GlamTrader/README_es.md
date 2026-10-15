# Estrategia GlamTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia GlamTrader** es una conversión de la API de alto nivel de StockSharp del asesor experto de MetaTrader `GlamTrader.mq5`. El robot original combina una media móvil desplazada con el oscilador Laguerre RSI y el Awesome Oscillator para filtrar el momentum antes de abrir una única posición de mercado. El puerto preserva el árbol de decisiones exacto y las reglas de gestión del dinero mientras adapta la ejecución de órdenes, la representación gráfica y los controles de riesgo a las convenciones de StockSharp.

## Cómo funciona la estrategia

1. Suscribirse a la serie de velas definida por `CandleType` (M15 por defecto). El marco temporal seleccionado alimenta cada indicador.
2. Construir una media móvil configurable en la fuente `AppliedPrice` seleccionada y desplazarla `MaShift` barras para reproducir el búfer desplazado usado en MetaTrader.
3. Recrear el filtro Laguerre RSI internamente usando el filtro recursivo de cuatro etapas (`LaguerreGamma` controla el factor de suavizado). El valor permanece en el rango `[0;1]` como el indicador personalizado original.
4. Calcular el Awesome Oscillator con promedios simples estándar de 5/34 del precio mediano y almacenar las lecturas actuales y anteriores para la detección de pendiente.
5. Solo cuando no hay ninguna posición abierta:
   - **Entrada larga** – media móvil por encima del cierre actual, Laguerre RSI por encima de `0.15`, y Awesome Oscillator subiendo respecto a la barra anterior.
   - **Entrada corta** – media móvil por debajo del cierre actual, Laguerre RSI por debajo de `0.75`, y Awesome Oscillator bajando respecto a la barra anterior.
6. Al entrar, la estrategia convierte las distancias de stop-loss/take-profit de pips a desplazamientos de precio usando el tamaño de tick del instrumento. Las distancias se ajustan para cotizaciones de 3 o 5 dígitos exactamente como `Point * 10` en MQL.
7. Mientras una posición está activa, el algoritmo refleja la rutina de trailing original: una vez que el precio avanza más de `TrailingStopPips + TrailingStepPips`, el stop se traillea a `TrailingStopPips` detrás (o por encima) del mercado. Las salidas se ejecutan cuando el rango de la vela toca el precio del trailing stop o del take-profit.

## Lógica de entrada y salida

- Mantener como máximo una posición en todo momento. Las señales opuestas se ignoran hasta que la operación actual se cierra.
- Las operaciones largas requieren una media móvil desplazada bajista (precio cruzando por encima de la línea), Laguerre RSI saliendo de la zona de sobreventa (`> 0.15`), y momentum del Awesome Oscillator creciente.
- Las operaciones cortas requieren una media móvil desplazada alcista (precio cruzando por debajo de la línea), Laguerre RSI cayendo de la zona de sobrecompra (`< 0.75`), y momentum del Awesome Oscillator decreciente.
- Los stops y objetivos se aplican con comparaciones de precio contra máximos/mínimos de vela para que los toques intrabarra sean respetados aunque la lógica se ejecute en velas terminadas.
- El trailing sigue la regla de MetaTrader: el stop solo se mueve después de que el precio avance la distancia del stop más el paso de trailing, y nunca retrocede.

## Parámetros

| Parámetro | Tipo | Predeterminado | Descripción |
|-----------|------|---------|-------------|
| `CandleType` | `DataType` | `TimeSpan.FromMinutes(15).TimeFrame()` | Marco temporal usado para los cálculos de indicadores y la toma de decisiones. |
| `TradeVolume` | `decimal` | `1` | Volumen usado para órdenes de mercado. |
| `StopLossBuyPips` | `decimal` | `50` | Distancia de stop-loss en pips para entradas largas. |
| `TakeProfitBuyPips` | `decimal` | `50` | Distancia de take-profit en pips para entradas largas. |
| `StopLossSellPips` | `decimal` | `50` | Distancia de stop-loss en pips para entradas cortas. |
| `TakeProfitSellPips` | `decimal` | `50` | Distancia de take-profit en pips para entradas cortas. |
| `TrailingStopPips` | `decimal` | `5` | Distancia del trailing stop en pips. Establezca en cero para deshabilitar el trailing. |
| `TrailingStepPips` | `decimal` | `15` | Beneficio adicional (en pips) requerido antes de que el trailing stop pueda moverse. |
| `MaPeriod` | `int` | `14` | Longitud de lookback de la media móvil. |
| `MaShift` | `int` | `1` | Desplazamiento positivo aplicado a la media móvil. |
| `MaMethod` | `MaMethod` | `LinearWeighted` | Tipo de media móvil (simple, exponencial, suavizada o ponderada linealmente). |
| `AppliedPrice` | `AppliedPrice` | `Weighted` | Fuente de precio usada para la media móvil y el filtro Laguerre. |
| `LaguerreGamma` | `decimal` | `0.7` | Coeficiente de suavizado Laguerre (rango 0–1). |

## Consejos de uso

1. Adjunte la estrategia al valor deseado, asegúrese de que el modelo de broker suministre información de tamaño/paso de tick, y establezca `CandleType` para que coincida con el marco temporal que desea operar.
2. Ajuste los parámetros de riesgo basados en pips a la volatilidad del instrumento. La conversión normaliza automáticamente las distancias usando `PriceStep`; los símbolos FX de cinco dígitos obtienen el multiplicador 10× esperado.
3. Los ayudantes de gráfico opcionales dibujan la media móvil en el área de precio y trazan el Awesome Oscillator en un panel separado junto con sus propias operaciones.
4. Inicie la estrategia. Gestionará los stops y el trailing internamente, reflejando las rutinas `OpenBuy`, `OpenSell` y de trailing del código MQL original.

## Notas

- La implementación del Laguerre RSI refleja el indicador `laguerre.mq5`, incluida la normalización `CU/(CU+CD)`.
- Los valores del Awesome Oscillator provienen del indicador incorporado de StockSharp, por lo que no se requiere copiar búferes manualmente.
- Dado que la lógica se evalúa en velas completadas, los backtests y el trading en vivo permanecen deterministas y libres de repintado a nivel de tick.
