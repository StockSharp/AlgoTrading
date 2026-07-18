# Estrategia Spasm
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Conversión del asesor experto MetaTrader 5 *Spasm (edición de barabashkakvn)* a la API de alto nivel de StockSharp.
- Opera rupturas de un canal adaptativo dimensionado por la volatilidad reciente y alterna entre regímenes alcistas y bajistas.
- Funciona en cualquier instrumento y marco temporal suministrado por el parámetro `CandleType`, con velas de una hora por defecto.

## Preparación de datos
1. Se suscribe a la serie de velas definida por `CandleType` para el instrumento de la estrategia.
2. Construye un estimador de volatilidad a partir de las últimas `VolatilityPeriod` velas:
   - Cuando `UseWeightedVolatility` está desactivado, el estimador es una media móvil simple del rango por vela.
   - Cuando `UseWeightedVolatility` está activado, el estimador se convierte en una media móvil ponderada linealmente que enfatiza las barras más recientes.
3. El rango por vela es `High - Low` por defecto. Si `UseOpenCloseRange` está activado, se utiliza la diferencia absoluta entre apertura y cierre, reproduciendo el cambio de modo del EA original.
4. El rango promedio bruto se convierte en pasos de precio y se multiplica por `VolatilityMultiplier`. El resultado se trunca a un número entero de pasos y finalmente se multiplica por el tamaño del tick del instrumento para formar el umbral de ruptura.
5. Durante las primeras `VolatilityPeriod * 3` velas terminadas, la estrategia recopila el máximo más alto y el mínimo más bajo junto con sus marcas de tiempo para decidir qué oscilación es más reciente. Esa información inicializa el estado de tendencia inicial y los precios de referencia una vez que se procesan suficientes velas.

## Parámetros
| Nombre | Valor predeterminado | Descripción |
| --- | --- | --- |
| `Volume` | `1` | Volumen de la orden aplicado a cada entrada de mercado. |
| `VolatilityMultiplier` | `5` | Multiplicador aplicado a la volatilidad promediada para dimensionar el buffer de ruptura. |
| `VolatilityPeriod` | `24` | Número de velas utilizadas para la rutina de promedio de volatilidad y el análisis inicial de oscilaciones. |
| `UseWeightedVolatility` | `false` | Cambia el promedio de volatilidad de media móvil simple a media móvil ponderada linealmente. |
| `UseOpenCloseRange` | `false` | Usa el movimiento absoluto apertura-cierre como fuente de volatilidad en lugar del rango máximo-mínimo. |
| `StopLossFraction` | `0.5` | Fracción del umbral de volatilidad empleado para calcular la distancia del stop-loss. Se impone un mínimo de tres pasos de precio. |
| `CandleType` | `marco temporal de 1 hora` | Tipo de vela y marco temporal usado para todos los cálculos. |

## Lógica de trading
1. **Seguimiento de tendencia**
   - La estrategia mantiene `_highestPrice` y `_lowestPrice` como anclas de la oscilación actual.
   - Siempre que el precio avance más del umbral actual por encima del máximo almacenado, `_highestPrice` se actualiza al máximo de la vela. Análogamente, una caída más allá del umbral actualiza `_lowestPrice` al mínimo de la vela.
   - El booleano `_isTrendUp` almacena si la estrategia está actualmente en el régimen alcista (true) o bajista (false).
2. **Reglas de entrada**
   - Cuando `_isTrendUp` es `false` (régimen bajista) y el cierre de la vela supera `_lowestPrice + threshold`, la estrategia cambia al modo alcista y envía `BuyMarket(Volume + Math.Abs(Position))`. Esto cierra cualquier exposición corta y abre una posición larga igual a `Volume`.
   - Cuando `_isTrendUp` es `true` (régimen alcista) y el cierre de la vela cae por debajo de `_highestPrice - threshold`, la estrategia cambia al modo bajista y envía `SellMarket(Volume + Math.Abs(Position))` para revertir a una posición corta.
3. **Gestión de stops**
   - Al entrar en una posición larga, el precio de stop se coloca en `entry - max(threshold * StopLossFraction, 3 * priceStep)`.
   - Al entrar en una posición corta, el precio de stop se coloca en `entry + max(threshold * StopLossFraction, 3 * priceStep)`.
   - Si el mínimo de una vela alcanza el stop largo o el máximo alcanza el stop corto, la posición correspondiente se cierra enviando una orden de mercado. Los stops se deshabilitan cuando `StopLossFraction` se establece en cero.
4. **Controles de riesgo e infraestructura**
   - `StartProtection()` se llama durante el inicio para que las protecciones de riesgo integradas se activen tan pronto como comience la estrategia.
   - La estrategia solo reacciona a las velas terminadas para evitar el ruido intrabarra, reflejando el recalculado barra a barra del EA original.
   - Todos los comentarios y nombres de parámetros se mantienen en inglés según los requisitos.

## Diferencias con la versión MQL
- El EA original recalculaba los umbrales en cada tick. En este puerto, la lógica se ejecuta en velas completadas porque la API de alto nivel opera con suscripciones de velas.
- La aplicación del stop-loss ocurre en datos de velas. Los golpes de stop intrabarra que se revierten dentro de la misma barra se evalúan por lo tanto en los límites de la vela.
- Las propiedades del símbolo como spread y niveles de stop específicos del bróker no están disponibles en la misma forma en StockSharp. Se usa un mínimo conservador de tres pasos de precio cuando la distancia de stop calculada es demasiado pequeña, reproduciendo el fallback de la implementación MetaTrader.

## Notas de uso
- Asegúrese de que el instrumento de la estrategia expone un `PriceStep` válido. Si no se proporciona, el código establece el paso en `1` por defecto.
- La estrategia es agnóstica en cuanto a la dirección y puede usarse en instrumentos spot, futuros o CFD siempre que el feed entregue las velas configuradas.
- No se define ningún objetivo de take-profit; las salidas ocurren solo mediante cambios de régimen o activación de stop-loss.
