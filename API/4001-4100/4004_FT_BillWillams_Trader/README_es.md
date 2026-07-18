# Estrategia comercial de FT Bill Williams
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **estrategia comercial **FT Bill Williams** es una traducción de alto nivel StockSharp del MetaTrader asesor experto "FT_BillWillams_Trader". Combina los fractales de Bill Williams con el indicador Alligator para negociar rupturas de tendencias. La estrategia busca fractales nuevos, verifica que la estructura Alligator confirme la dirección de ruptura y, opcionalmente, aplica filtros de distancia, alineación y señal inversa antes de abrir una posición.

## Lógica de trading

1. **Detección de fractales**: la estrategia almacena los máximos y mínimos más recientes de `FractalPeriod`. Cuando la barra del medio es el punto más alto (o más bajo) de la ventana, se registra un nuevo nivel de ruptura. Se agrega un desplazamiento `IndentPoints` encima/debajo del fractal para evitar entradas prematuras.
2. **Confirmación de ruptura** – dependiendo de `EntryConfirmation`:
   - `PriceBreakout` confirma cuando el rango de velas cruza el nivel de ruptura.
   - `CloseBreakout` espera a que el cierre de la vela anterior supere el nivel.
3. **Verificación de distancia**: las entradas se rechazan cuando el nivel de ruptura está a más de `MaxDistancePoints` de los labios Alligator (valor de barra anterior). Establezca la distancia en cero para desactivar el filtro.
4. **Filtro de dientes**: cuando `UseTeethFilter` está habilitado, el cierre anterior debe estar por encima (para largos) o por debajo (para cortos) de los dientes Alligator.
5. **Alineación de tendencia**: con `UseTrendAlignment = true`, los labios, los dientes y la mandíbula deben estar separados por al menos `TeethLipsDistancePoints` y `JawTeethDistancePoints` puntos, respectivamente, lo que confirma que Alligator es tendencia.
6. **Salidas inversas**: si `ReverseExit = OppositeFractal`, cualquier nuevo fractal opuesto cierra inmediatamente la posición abierta. Con `OppositePosition`, la estrategia primero cierra la operación actual antes de abrir una en la dirección opuesta.
7. **Salida de la mandíbula**: `JawExit` define si la posición se cierra cuando el precio cruza la mandíbula Alligator (intrabar o al cierre de la vela).
8. **Trailing stop**: cuando `EnableTrailing` es verdadero y la operación es rentable, el stop se mueve hacia los labios o los dientes dependiendo de la pendiente relativa de los labios y el `SlopeSmaPeriod` SMA. Las paradas de protección iniciales y los objetivos de ganancias están controlados por `StopLossPoints` y `TakeProfitPoints`.

## Parámetros

| Propiedad | Descripción | Predeterminado |
|----------|-------------|---------|
| `OrderVolume` | Volumen comercial utilizado al enviar órdenes de mercado. | `0.1` |
| `FractalPeriod` | Número de barras en el patrón fractal (se recomiendan valores impares). | `5` |
| `IndentPoints` | Compensación agregada al nivel de ruptura (en puntos). | `1` |
| `EntryConfirmation` | Modo de confirmación de ruptura (`PriceBreakout`, `CloseBreakout`). | `CloseBreakout` |
| `UseTeethFilter` | Requiere que el cierre anterior esté en el lado correcto de los dientes Alligator. | `true` |
| `MaxDistancePoints` | Distancia máxima entre el nivel de ruptura y Alligator labios (puntos). | `1000` |
| `UseTrendAlignment` | Aplicar una separación mínima entre Alligator líneas. | `false` |
| `JawTeethDistancePoints` | Distancia mínima entre mandíbula y dientes utilizada en el filtro de alineación. | `10` |
| `TeethLipsDistancePoints` | Distancia mínima entre dientes y labios utilizada en el filtro de alineación. | `10` |
| `JawExit` | Modo de cierre de posiciones en cruce de mandíbulas (`Disabled`, `PriceCross`, `CloseCross`). | `CloseCross` |
| `ReverseExit` | Manejo de señales opuestas (`Disabled`, `OppositeFractal`, `OppositePosition`). | `OppositePosition` |
| `EnableTrailing` | Habilite la gestión de trailing stop basada en Alligator. | `true` |
| `SlopeSmaPeriod` | Período del SMA que se compara con la pendiente de los labios. | `5` |
| `StopLossPoints` | Distancia de stop-loss en puntos (0 inhabilitaciones). | `50` |
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos (0 inhabilitaciones). | `50` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Períodos para las líneas Alligator. | `13`, `8`, `5` |
| `JawShift`, `TeethShift`, `LipsShift` | Desplazamiento hacia adelante para cada línea Alligator. | `8`, `5`, `3` |
| `MaMethod` | Tipo de media móvil para Alligator (`Simple`, `Exponential`, `Smoothed`, `Weighted`). | `Simple` |
| `AppliedPrice` | Precio de vela suministrado al Alligator. | `CandlePrice.Median` |
| `CandleType` | Tipo de vela suscrita a partir de los datos del mercado. | `15-minute timeframe` |

## Notas adicionales

- La estrategia dibuja las líneas Alligator y ejecuta operaciones en el área del gráfico predeterminada.
- `FractalPeriod` debe permanecer impar para que la barra central represente el vértice fractal; el valor predeterminado coincide con el asesor experto original.
- Los parámetros basados en la distancia (`IndentPoints`, `MaxDistancePoints`, `JawTeethDistancePoints`, `TeethLipsDistancePoints`, `StopLossPoints`, `TakeProfitPoints`) se expresan en puntos de corredor (`Security.PriceStep`).
- Las paradas dinámicas y las salidas de mandíbula dependen de velas completadas, lo que refleja la lógica MQL original que funciona con los valores de barra anteriores del Alligator.
