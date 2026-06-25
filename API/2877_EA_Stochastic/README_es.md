# Estrategia EA Stochastic
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Port de alto nivel para StockSharp del asesor experto MetaTrader "EA Stochastic". La estrategia se suscribe a una serie de velas, lee
los valores del oscilador estocástico y mantiene como máximo una posición neta. Las entradas se producen cuando la línea principal del
estocástico ha permanecido en el mismo lado de los umbrales configurados durante un número configurable de barras. Las salidas de
protección y un stop de seguimiento replican la implementación MQL original usando distancias basadas en pips.

## Descripción general de la estrategia

- **Indicador**: oscilador estocástico clásico (componentes `%K` y `%D` con suavizado configurable)
- **Dirección**: largo y corto
- **Posicionamiento**: una sola posición a la vez (las nuevas operaciones se ignoran mientras hay una orden de salida pendiente)
- **Tipo de orden**: órdenes de mercado usando volumen fijo
- **Datos**: un solo tipo de vela seleccionado por el usuario (predeterminado velas de 15 minutos)

## Lógica de entrada

1. El valor principal del estocástico se almacena en cada vela completada.
2. Después de que al menos `ComparedBar` valores estén en caché, comparar el `kValue` actual con el valor de `ComparedBar - 1` velas atrás.
3. **Ir Largo** cuando ambos valores estén por debajo de `UpperLevel`. Esto coincide con el EA original que solo compra cuando el oscilador se ha mantenido
   por debajo del umbral superior durante la longitud de lookback configurada.
4. **Ir Corto** cuando ambos valores estén por encima de `LowerLevel`. El EA original permitía cortos siempre que el estocástico se mantuviera por encima del límite
   inferior.
5. Las nuevas entradas se omiten si existe una posición o si ya se ha solicitado una salida de protección para la posición actual.

## Salida y gestión de riesgo

- **Stop Loss**: distancia opcional fija de pips desde el precio de entrada. Los stops se evalúan contra los mínimos de vela (para largos) o máximos
  (para cortos).
- **Take Profit**: objetivo fijo opcional en pips. Las comprobaciones de alto/bajo emulan el comportamiento del take profit basado en órdenes de MetaTrader.
- **Stop de Seguimiento**: activado una vez que la operación abierta gana más de `(TrailingStopPips + TrailingStepPips)` pips. El stop se mueve entonces
  a `TrailingStopPips` detrás del último extremo, respetando el gap del paso de seguimiento igual que el EA original.
- **Órdenes de salida**: los cierres se emiten con órdenes de mercado (`SellMarket` / `BuyMarket`). Un indicador de guarda previene órdenes de salida repetidas
  hasta que `OnPositionChanged` confirme el estado plano.

## Parámetros

- `StopLossPips` (predeterminado **50**): distancia en pips usada para el stop de protección inicial. Establecer en cero para deshabilitar.
- `TakeProfitPips` (predeterminado **150**): distancia en pips para toma de ganancias. Establecer en cero para deshabilitar.
- `TrailingStopPips` (predeterminado **15**): distancia de seguimiento en pips. Debe ser mayor que cero si el seguimiento está habilitado.
- `TrailingStepPips` (predeterminado **5**): progreso mínimo en pips requerido antes de actualizar el stop de seguimiento. El seguimiento se rechaza cuando
  este valor es cero.
- `Volume` (predeterminado **1**): volumen de orden de mercado usado para operaciones largas y cortas.
- `KPeriod` (predeterminado **5**): longitud de lookback para la línea estocástica %K.
- `DPeriod` (predeterminado **3**): longitud de suavizado para la línea %D.
- `Slowing` (predeterminado **3**): suavizado final aplicado al cálculo de %K.
- `UpperLevel` (predeterminado **80**): umbral usado para validar configuraciones largas.
- `LowerLevel` (predeterminado **20**): umbral usado para validar configuraciones cortas.
- `ComparedBar` (predeterminado **3**): número de barras a mirar atrás al validar los niveles estocásticos (mínimo 1).
- `CandleType` (predeterminado **velas de 15 minutos**): serie de velas a la que se suscribe la estrategia.

## Notas de implementación

- El tamaño de pip se aproxima desde `Security.PriceStep`. Para instrumentos con pips fraccionarios (pares FX típicos) los pasos menores que
  `0.001` se multiplican automáticamente por 10, reproduciendo la lógica `digits_adjust` de MetaTrader.
- La configuración del stop de seguimiento se valida al inicio para evitar el caso de error del EA original (`TrailingStop > 0` con paso de seguimiento cero).
- El oscilador estocástico de StockSharp usa suavizado predeterminado y modos de precio (cierre/alto/bajo), que se corresponde con la configuración del EA de
  media móvil simple sobre rangos alto/bajo.
- El EA original proporcionaba tanto lote fijo como dimensionamiento de posición por porcentaje de riesgo. Este port mantiene el parámetro fijo `Volume` y puede
  extenderse si se requiere dimensionamiento basado en porcentaje.
- La salida del gráfico renderiza las velas procesadas, el indicador estocástico y las operaciones ejecutadas para facilitar la depuración.

## Uso sugerido

- Funciona en marcos temporales intradía o superiores; ajuste `CandleType` y los períodos del estocástico para adaptarse al instrumento.
- Ajuste `UpperLevel`, `LowerLevel` y `ComparedBar` para diferentes regímenes de mercado (rango vs. tendencia).
- Combine con controles de riesgo del lado del broker en operaciones en vivo porque las salidas se simulan a través de órdenes de mercado después de la
  confirmación de vela.
