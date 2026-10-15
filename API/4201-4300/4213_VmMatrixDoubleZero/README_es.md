# VmMatrix Doble Cero
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
VmMatrix Double Zero es un StockSharp puerto del MetaTrader 4 asesor experto `vMATRIXDoubleZero`. El robot original busca rupturas de "doble cero" redondeando la vela anterior cerca de dos decimales e ingresando operaciones cuando el precio cruza ese nivel redondeado. El puerto mantiene la estructura de filtro en capas del EA: comparaciones de polarización de múltiples barras configurables, comprobaciones opcionales de volumen y rango, una puerta de aceleración ATR y un filtro secundario de fuerza de oscilación. La estrategia también puede requerir que el índice diario del canal de productos básicos (CCI) confirme la dirección y ofrece un componente adaptable de toma de ganancias derivado de estadísticas horarias ATR.

El comercio se limita a una ventana de tiempo de terminal definida por el usuario, y controles separados controlan si se pueden realizar configuraciones largas o cortas. Las paradas y los objetivos se gestionan internamente, incluida una aproximación del comportamiento original del trailing-stop que amplía el nivel de obtención de beneficios cada vez que se habilita el trailing.

## Lógica estratégica
### Detección de sesgos
* **Ruptura redondeada**: el disparador central compara el cierre de las dos últimas velas terminadas con el cierre anterior redondeado a dos decimales. Una señal larga requiere `Close[2] < round(Close[1], 2)` y `Close[1] > round(Close[1], 2)`; Las señales cortas invierten las desigualdades.
* **Filtro de matriz (opcional)**: cuando está habilitado, seis velas históricas definidas por los parámetros `LongK1…LongK6` (para largos) o `ShortK1…ShortK6` (para cortos) se comparan utilizando desviaciones del punto medio. Cada desviación se calcula como `Close - (High + Low) / 2`. Las comparaciones reflejan el EA original y requieren que la primera desviación domine a la segunda, la tercera supere un cuarto en escala multiplicadora (`LongQc`/`ShortQc`) y la quinta supere un segundo sexto en escala multiplicadora (`LongQg`/`ShortQg`).

### Filtros adicionales
* **Filtro de sesión**: las operaciones solo se evalúan cuando la hora de cierre de la vela procesada cae entre `StartHour` y `EndHour` (inclusive).
* **Filtro de volumen**: si está habilitado, el volumen total de la vela anterior debe exceder `MinimumVolume`.
* **Compresión de rango**: el máximo más alto y el mínimo más bajo de las últimas `RangeBars` velas deben estar dentro de `RangeThresholdPips` pips.
* **ATR aceleración**: compara el valor ATR más reciente (longitud `AtrPeriod` en el período de trabajo) con el valor ATR hace `AtrShift` barras. La señal se acepta solo si el ATR actual es más alto, imitando el cambio VSA del EA.
* **Filtro de oscilación secundario**: cuando está activo, una suma ponderada de diferencias altas/bajas creadas a partir de la retrospectiva `SecondaryPivot` debe ser positiva para posiciones largas o negativa para posiciones cortas. Los pesos (`Xb2`, `Xs2`, `Yb2`, `Ys2`) siguen el esquema de parámetros original donde 50 representa la neutralidad.
* **Confirmación diaria de CCI**: puerta opcional que requiere que el valor diario de CCI más reciente (período `DailyCciPeriod`) esté por encima de cero para posiciones largas o por debajo de cero para posiciones cortas.

### Gestión de pedidos
* **Tamaño de entrada**: los pedidos usan `OrderVolume` ajustado al paso de volumen del valor. Si ya hay una posición opuesta abierta, la estrategia opcionalmente la cierra primero (`CloseOnBiasFlip` debe ser verdadero); de lo contrario, se omitirá la nueva entrada porque el puerto se ejecuta en un entorno de red.
* **Paradas iniciales**: las distancias de parada y pérdida se expresan en pips hasta `LongStopLossPips`/`ShortStopLossPips` y se convierten utilizando el tamaño de pip detectado. Las distancias de obtención de beneficios utilizan `LongTakeProfitPips`/`ShortTakeProfitPips` y pueden aumentarse mediante el componente dinámico siguiente.
* **Toma de ganancias dinámica**: cuando `UseDynamicTakeProfit` está habilitado, la estrategia agrega una combinación ponderada de estadísticas horarias ATR y diferencias de oscilación a la distancia base de toma de ganancias. La contribución refleja la función `TPb()` del EA: combina el cambio en el ATR(1) por hora, el último ATR(1) por hora, el ATR(25) por hora y la diferencia entre los máximos separados por barras `SwingPivot`. Todos los pesos están centrados alrededor de 50, coincidiendo con la interfaz original.
* **Trailing stop**: habilitar `UseTrailingStop` activa un trailing stop de estilo escalonado que aumenta (o reduce) el nivel de stop cada vez que el precio recorre aproximadamente el doble de la distancia de stop configurada más allá del stop actual. Al igual que en la versión MQL, la distancia de obtención de beneficios se multiplica por 10 para mantener la operación abierta de forma efectiva mientras el seguimiento está activo.
* **Salidas protectoras**: en cada vela terminada, la estrategia comprueba si se ha superado el stop-loss o el take-profit. En respuesta, las posiciones se cierran en el mercado. Un cambio de polarización (`CloseOnBiasFlip`) también cierra la posición actual si se detecta la señal opuesta.

## Parámetros
La siguiente tabla resume los parámetros expuestos (todos están disponibles para optimización a menos que se indique lo contrario):

| grupo | Parámetro | Descripción |
| --- | --- | --- |
| generales | `StartHour` / `EndHour` | Ventana de negociación inclusiva en horario terminal. |
| generales | `OrderVolume` | Tamaño del pedido base, normalizado al paso de volumen del instrumento. |
| generales | `UseTrailingStop` | Habilita la aproximación del trailing-stop y amplía el factor de obtención de beneficios para emular el EA. |
| generales | `CloseOnBiasFlip` | Si es cierto, cierra la exposición opuesta antes de iniciar una nueva operación. |
| Largo / Corto | `EnableLongs` / `EnableShorts` | Alterna el procesamiento de señales largas o cortas. |
| Largo / Corto | `LongStopLossPips`, `LongTakeProfitPips`, `ShortStopLossPips`, `ShortTakeProfitPips` | Distancias de stop-loss y take-profit medidas en pips. |
| Filtros | `UseBiasFilter` with `LongK1…LongK6`, `ShortK1…ShortK6`, `LongQc`, `LongQg`, `ShortQc`, `ShortQg` | Configura las comparaciones de desviación de estilo matricial para señales largas y cortas. |
| Filtros | `UseRangeFilter`, `RangeBars`, `RangeThresholdPips` | Rechaza operaciones cuando el rango de precios reciente excede el umbral de pips. |
| Filtros | `UseVolumeFilter`, `MinimumVolume` | Requiere que el volumen de la vela anterior supere el umbral. |
| Filtros | `UseVsaFilter`, `AtrPeriod`, `AtrShift` | Demandas que ATR ha aumentado en relación con hace `AtrShift` barras. |
| Filtros | `UseSecondaryFilter`, `Xb2`, `Xs2`, `Yb2`, `Ys2`, `SecondaryPivot` | Filtro ponderado de intensidad de oscilación basado en máximos y mínimos. |
| Filtros | `UseDailyCciFilter`, `DailyCciPeriod` | Puerta CCI diaria; Los largos necesitan CCI positivo, los cortos necesitan CCI negativo. |
| Tomar ganancias | `UseDynamicTakeProfit`, `WeightSn1…WeightSn4`, `SwingPivot` | Controla el componente de toma de ganancias adaptativo que combina métricas ATR horarias y distancias de swing. |
| generales | `CandleType` | Periodo de tiempo principal que impulsa todos los cálculos de señales. |

## Notas adicionales
* El tamaño del pip se infiere de `Security.PriceStep`. Los símbolos FX de cinco y tres dígitos se asignan automáticamente a un multiplicador de 10×, reflejando el manejo de MQL de `Digits` y `Point`.
* El puerto se suscribe a tres flujos de datos: el período de trabajo, velas por hora (para cálculos de ATR) y velas diarias (para CCI). Asegúrese de que el proveedor de datos pueda proporcionar todos los plazos solicitados.
* Debido a que las estrategias StockSharp operan sobre posiciones netas, no se admite cubrir el mismo instrumento en ambas direcciones simultáneamente. Habilite `CloseOnBiasFlip` para imitar la capacidad de EA de cerrarse y revertirse rápidamente.
* El comportamiento del trailing-stop es aproximado; el EA utilizó valores de diferencial sin procesar para determinar el paso final. El puerto requiere que el precio recorra aproximadamente el doble de la distancia de la parada antes de avanzar hacia la parada, lo que produce un resultado similar sin información explícita sobre el diferencial.
