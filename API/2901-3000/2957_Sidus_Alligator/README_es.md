# Estrategia Sidus Alligator
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Sidus reproduce la lógica clásica del asesor experto "Sidus" de MetaTrader en StockSharp. Combina el indicador Alligator de Bill Williams con un filtro de Índice de Fuerza Relativa (RSI) de 14 períodos. El sistema busca un cruce RSI por encima o por debajo de la línea media 50 mientras las tres medias móviles del Alligator se expanden en la misma dirección. Cada entrada calcula inmediatamente stops de protección y gestión opcional de trailing expresada en distancias de pips que respetan el paso de precio del instrumento.

## Indicadores y datos
- **Líneas del Alligator**: tres medias móviles suavizadas calculadas sobre el precio mediano de la vela (máximo + mínimo ÷ 2) con longitudes y desplazamientos hacia adelante independientes para la mandíbula, los dientes y los labios. Se comparan valores consecutivos para detectar expansión hacia arriba o hacia abajo.
- **Índice de Fuerza Relativa (RSI)**: un oscilador de 14 períodos evaluado en precios de cierre. Solo las velas terminadas participan en la decisión para evitar sesgo de anticipación.
- **Velas**: se puede seleccionar cualquier marco temporal mediante el parámetro `CandleType`. Por defecto, la estrategia usa velas de marco temporal de un minuto.

## Lógica de trading
1. **Confirmación RSI**
   - Configuración larga: RSI cruza hacia arriba por 50 (`RSI[t-2] < 50` y `RSI[t-1] > 50`).
   - Configuración corta: RSI cruza hacia abajo por 50 (`RSI[t-2] > 50` y `RSI[t-1] < 50`).
2. **Filtro de pendiente del Alligator**
   - La entrada larga requiere que las pendientes de la mandíbula, los dientes y los labios entre los dos valores completados anteriores (teniendo en cuenta los desplazamientos) superen el umbral `Delta`.
   - La entrada corta requiere que las mismas pendientes estén por debajo del umbral, indicando compresión o declive.
3. **Manejo de posiciones**
   - Cuando aparece una señal larga, los cortos se cierran primero si `CloseOpposite = true`. La estrategia luego compra el `OrderVolume` configurado a mercado.
   - Cuando aparece una señal corta, los largos se aplanan si lo permite `CloseOpposite`, seguido de una venta de mercado de `OrderVolume`.

## Salida y gestión de riesgos
- **Stop-loss inicial**: calculado desde el extremo de la vela anterior menos/más `OffsetPips` (convertido usando el paso de precio del instrumento). Los stops se omiten si el nivel calculado invalidaría la operación (por ejemplo, distancia no positiva).
- **Take-profit**: distancia opcional definida por `TakeProfitPips`. Establecer el parámetro en cero deshabilita el objetivo.
- **Trailing stop**: si `TrailingStopPips` y `TrailingStepPips` son ambos positivos, el stop avanza una vez que el precio se mueve al menos `TrailingStopPips + TrailingStepPips` en favor de la posición. El nuevo stop se coloca a `TrailingStopPips` del máximo más alto (largos) o mínimo más bajo (cortos) alcanzado durante la barra.
- **Lógica de aplanamiento**: el stop-loss, take-profit y la lógica de trailing se evalúan en cada vela terminada usando rangos de máximo/mínimo para simular toques dentro de la barra.

## Parámetros
- `OrderVolume` (predeterminado **0.1**): tamaño del trade en lotes o contratos.
- `OffsetPips` (predeterminado **3**): distancia desde el extremo de la vela anterior al stop-loss. Cero deshabilita el stop inicial.
- `TakeProfitPips` (predeterminado **75**): distancia del take-profit. Cero deshabilita el objetivo.
- `TrailingStopPips` (predeterminado **5**): distancia del trailing stop. Debe ser positivo si el trailing está habilitado.
- `TrailingStepPips` (predeterminado **15**): movimiento adicional requerido antes de que el trailing stop avance. Debe ser positivo cuando el trailing está habilitado.
- `Delta` (predeterminado **0.00003**): diferencia mínima de pendiente para cada línea del Alligator entre muestras consecutivas.
- `CloseOpposite` (predeterminado **false**): si es `true`, las posiciones opuestas se cierran antes de abrir un nuevo trade; si es `false`, la estrategia espera a que la posición actual se aplane naturalmente.
- `JawPeriod`, `TeethPeriod`, `LipsPeriod`: longitudes de las medias móviles suavizadas para la mandíbula, los dientes y los labios del Alligator (predeterminados 13/8/5).
- `JawShift`, `TeethShift`, `LipsShift`: desplazamientos hacia adelante (predeterminados 8/5/3) usados al recuperar comparaciones de pendientes.
- `RsiPeriod` (predeterminado **14**): ventana de promediado RSI.
- `CandleType`: tipo de datos de vela/marco temporal a suscribir (predeterminado 1 minuto).

## Notas de implementación
- Las distancias basadas en pips se adaptan automáticamente a la precisión de precio del instrumento: los instrumentos de cinco y tres decimales multiplican el paso de precio por diez para coincidir con la definición de pip de MQL.
- Las verificaciones de pendiente del Alligator se basan en valores históricos almacenados que respetan los desplazamientos hacia adelante configurados, evitando la gestión manual de arrays más allá de un buffer de anillo mínimo.
- Las órdenes se ejecutan con los helpers de alto nivel `BuyMarket` y `SellMarket`, manteniendo la estrategia enfocada en la generación de señales mientras StockSharp maneja el enrutamiento.
