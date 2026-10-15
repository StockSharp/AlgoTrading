# Estrategia Dematus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia Dematus replica la lógica del asesor experto original "Dematus" de MetaTrader 5. Utiliza el oscilador DeMarker para detectar reversiones de momentum y admite la piramidación con dimensionamiento adaptativo de posiciones. La estrategia está diseñada para un único instrumento y opera en la serie de velas definida por el parámetro `CandleType`.

En cada vela terminada se evalúan dos valores del DeMarker: el valor más reciente y el valor de dos barras atrás. Un cruce desde el umbral de sobreventa (0.3) hacia arriba señala oportunidades largas, mientras que un cruce desde el umbral de sobrecompra (0.7) hacia abajo señala oportunidades cortas. Después de una entrada inicial, la estrategia puede añadir a la posición si el precio viaja una distancia configurable desde el último precio de entrada ejecutado y la señal del DeMarker se activa nuevamente.

## Reglas de trading
- **Entrada primaria:**
  - Abrir una posición larga cuando el valor del DeMarker de dos barras atrás está por debajo de 0.3 y el valor actual sube por encima de 0.3, siempre que no haya posición abierta.
  - Abrir una posición corta cuando el valor del DeMarker de dos barras atrás está por encima de 0.7 y el valor actual cae por debajo de 0.7, siempre que no haya posición abierta.
- **Lógica de escalado:**
  - Mientras una posición está activa, la estrategia recuerda el precio exacto del último llenado. Si el precio se mueve en contra de la posición al menos `DistancePips` (convertido a unidades de precio) y el cruce de DeMarker correspondiente ocurre nuevamente, la estrategia envía una orden adicional en la misma dirección.
  - El tamaño de cada orden adicional es el volumen ejecutado anterior multiplicado por `VolumeMultiplier`, redondeado al paso de volumen del instrumento y restringido por los límites del mercado. Esto refleja el comportamiento del coeficiente de lote del asesor experto original.
- **Gestión de stops:**
  - Se adjunta un stop loss inicial a cada nueva posición usando `StopLossPips`. El nivel de stop se recalcula después de cada trade de escalado para que la posición neta consolidada siempre tenga un nivel de protección válido.
  - Si `TrailingStopPips` está habilitado, el nivel de stop se ajusta cuando el beneficio abierto supera `TrailingStopPips + TrailingStepPips`, emulando la lógica de trailing stop de la implementación MQL.
- **Protección de patrimonio:**
  - Cuando está plano, la estrategia define un piso de patrimonio virtual igual a `Balance - VirtualStopEquity`.
  - Una vez que el patrimonio flotante sube al menos `TrailingStartEquity`, se activa un stop de patrimonio en trailing y sigue el patrimonio pico menos `TrailingEquity`.
  - Si el patrimonio de la cuenta cae por debajo del piso virtual mientras una posición está abierta, todas las posiciones se liquidan inmediatamente.

## Parámetros
| Parámetro | Descripción |
| --- | --- |
| `InitialVolume` | Tamaño de orden base para el primer trade. Se usa nuevamente cuando la posición está completamente cerrada. |
| `DemarkerLength` | Período del indicador DeMarker. |
| `StopLossPips` | Distancia del stop protector en pips aplicada a cada entrada. Establecer en cero para deshabilitar el stop loss estático. |
| `TrailingStopPips` | Distancia del trailing stop en pips. Establecer en cero para deshabilitar el trailing. |
| `TrailingStepPips` | Movimiento favorable adicional (en pips) requerido antes de que el trailing stop se mueva. Debe ser positivo cuando el trailing está activo. |
| `DistancePips` | Distancia de precio mínima (en pips) desde el último llenado antes de escalar en la posición. |
| `TrailingEquity` | Distancia entre el pico de patrimonio y el piso de patrimonio protector. |
| `VirtualStopEquity` | Buffer inicial por debajo del balance utilizado para calcular el piso de patrimonio virtual cuando la estrategia está plana. |
| `TrailingStartEquity` | Umbral de beneficio sobre el balance que activa el trailing de patrimonio. |
| `VolumeMultiplier` | Multiplicador aplicado al tamaño de la última orden ejecutada cuando se hace piramidación. |
| `ResetEntryPrice` | Cuando está habilitado, limpia el precio de entrada almacenado después de cada salida, evitando el escalado hasta que ocurra un nuevo trade. |
| `CandleType` | Tipo de datos de vela (marco temporal) utilizado para cálculos de indicadores y generación de señales. |

## Notas de implementación
- La estrategia se implementa con la API de alto nivel de StockSharp. Las suscripciones de velas se manejan a través de `SubscribeCandles`, y el indicador DeMarker se vincula mediante `Bind` para que los valores del indicador lleguen como decimales listos para usar.
- El estado del indicador se rastrea con variables escalares simples: el valor más reciente, el valor anterior y el valor de dos barras atrás, reflejando exactamente el patrón de acceso al buffer del código fuente MQL (`iDeMarkerGet(0)` e `iDeMarkerGet(2)`).
- Los volúmenes de órdenes se redondean según el paso de volumen del instrumento y se validan contra límites mínimos y máximos para prevenir rechazos.
- El control de patrimonio usa `Portfolio.CurrentValue` para reflejar las comprobaciones de balance/patrimonio presentes en el código original. Cuando se activa el stop basado en patrimonio, la estrategia cierra todas las posiciones abiertas a través de órdenes de mercado.
- El tamaño del pip se deriva de `Security.PriceStep`. Los instrumentos con tres o cinco decimales reciben automáticamente el ajuste de diez veces utilizado en la versión MQL para convertir puntos a pips.

## Notas de uso
- Asegúrese de que la cartera conectada suministre información de patrimonio actual para que la lógica de trailing de patrimonio opere correctamente.
- La estrategia opera solo en velas terminadas (`CandleStates.Finished`). Ignorará las barras parcialmente formadas, coincidiendo con la lógica de control de "nueva barra" del asesor experto original.
- Los umbrales predeterminados (0.3/0.7) están integrados en el código pero se pueden ajustar modificando las constantes si es necesario.
- La estrategia admite trading en vivo y backtesting. Para backtests, verifique que el simulador de cartera alimente valores de patrimonio para permitir que se ejecute la lógica de trailing de patrimonio.
