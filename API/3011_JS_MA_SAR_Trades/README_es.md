# Estrategia de Operaciones JS MA SAR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

JS MA SAR Trades convierte el experto de MetaTrader 5 "JS MA SAR Trades" en la API de alto nivel de StockSharp. La estrategia busca mínimos oscilantes más altos o máximos oscilantes más bajos detectados mediante un filtro estilo ZigZag, confirma el impulso con dos medias móviles y luego entra en la dirección de una ruptura del Parabolic SAR. Las posiciones se protegen con stops clásicos, stops trailing opcionales y un filtro de sesión de trading explícito.

## Descripción General de la Lógica

1. **Estructura de oscilaciones** – Los indicadores Highest/Lowest con la profundidad configurada aproximan el ZigZag original. Se rastrean los dos mínimos y máximos oscilantes más recientes. Una configuración larga requiere que el último mínimo sea más alto que el anterior (estructura ascendente), mientras que una configuración corta requiere que el último máximo sea más bajo que el anterior (estructura descendente). Un filtro de desviación (en pips) y un backstep mínimo (barras entre pivotes) evitan que los pivotes de ruido sean aceptados.
2. **Confirmación de media móvil** – Ambas medias móviles usan el mismo tipo de suavizado y precio aplicado que la versión MT5, incluyendo desplazamientos positivos opcionales (barras a la derecha). Una señal larga necesita que la MA rápida desplazada permanezca por encima de la MA lenta desplazada; una señal corta requiere la relación opuesta.
3. **Activador Parabolic SAR** – Una vez que se satisfacen las condiciones de oscilación y media móvil, la operación se ejecuta solo si la vela cierra más allá del nivel del Parabolic SAR: cierre por encima del SAR para largos y cierre por debajo para cortos. Los giros del SAR hacia el otro lado cierran todas las posiciones existentes incluso fuera de la ventana de entrada.
4. **Gestión de riesgos** – Los niveles de stop-loss y take-profit se calculan en pips (convertidos mediante el paso de precio del instrumento). El stop trailing opcional imita la lógica de MT5: el stop solo se desplaza después de que el precio se ha movido la distancia configurada de trailing stop más trailing step desde el precio de entrada.
5. **Filtro de sesión** – Cuando está habilitado, las órdenes solo se permiten entre las horas de inicio y fin especificadas (inclusive). Las salidas de protección (stop/take/trailing y reversión SAR) aún se evalúan en cada vela terminada.

## Reglas de Entrada y Salida

- **Entrada larga**: mínimo oscilante más alto, Parabolic SAR por debajo del cierre, MA rápida (con desplazamiento) por encima de la MA lenta, y cierre dentro de la ventana de trading. La estrategia compra `OrderVolume + |Position|` para cerrar cortos y abrir la posición larga.
- **Entrada corta**: máximo oscilante más bajo, Parabolic SAR por encima del cierre, MA rápida (con desplazamiento) por debajo de la MA lenta, y filtro de tiempo satisfecho.
- **Salida larga**:
  - El precio de cierre cruza por debajo del Parabolic SAR;
  - Se alcanza el nivel de stop-loss, trailing stop o take-profit.
- **Salida corta**:
  - El precio de cierre cruza por encima del Parabolic SAR;
  - Se alcanza el nivel de stop-loss, trailing stop o take-profit.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `OrderVolume` | `1` | Tamaño base de la orden para nuevas entradas; la estrategia añade la posición actual absoluta para revertir instantáneamente. |
| `StopLossPips` | `50` | Distancia en pips entre el precio de entrada y el stop-loss. Establecer en `0` para deshabilitar. |
| `TakeProfitPips` | `50` | Distancia en pips entre el precio de entrada y el take-profit. Establecer en `0` para deshabilitar. |
| `TrailingStopPips` | `5` | Distancia de trailing stop en pips. Trabaja junto con `TrailingStepPips`. |
| `TrailingStepPips` | `5` | Distancia adicional que el precio debe recorrer (en pips) antes de ajustar el trailing stop. Debe ser positivo cuando el trailing está habilitado. |
| `UseTimeFilter` | `true` | Habilitar el filtro de hora de inicio/fin para nuevas entradas. |
| `StartHour` | `19` | Inicio de la ventana de trading (inclusive, hora de la bolsa). |
| `EndHour` | `22` | Fin de la ventana de trading (inclusive). |
| `FastMaPeriod` | `55` | Período de la media móvil rápida. |
| `FastMaShift` | `3` | Desplazamiento adelantado (en barras) aplicado a los valores de la media móvil rápida. |
| `SlowMaPeriod` | `120` | Período de la media móvil lenta. |
| `SlowMaShift` | `0` | Desplazamiento adelantado (en barras) para la media móvil lenta. |
| `MaType` | `Smoothed` | Método de suavizado de la media móvil (Simple, Exponential, Smoothed, Weighted). |
| `AppliedPrice` | `Median` | Fuente de precio para ambas medias móviles (Close, Open, High, Low, Median, Typical, Weighted). |
| `SarStep` | `0.02` | Factor de aceleración inicial del Parabolic SAR. |
| `SarMax` | `0.2` | Factor de aceleración máximo del Parabolic SAR. |
| `ZigZagDepth` | `12` | Ventana de retrospectiva (barras) para la detección de oscilaciones. |
| `ZigZagDeviation` | `5` | Tamaño mínimo de oscilación medido en pips para aceptar un nuevo pivote. |
| `ZigZagBackstep` | `3` | Número mínimo de barras entre pivotes consecutivos del mismo tipo. |
| `CandleType` | `H1` | Marco temporal de trading para la suscripción de velas. |

## Notas

- La estrategia mantiene activa la lógica de protección incluso fuera de la ventana de entrada, asegurando que los stops y los giros del SAR sean respetados.
- El trailing stop reproduce la implementación de MT5: una vez que el precio avanza `TrailingStop + TrailingStep`, el stop se mueve a `Close - TrailingStop` para largos (espejado para cortos).
- Las medias móviles se evalúan sobre el precio aplicado seleccionado; el desplazamiento emula el offset del indicador MT5.
- Asegúrese de que el instrumento tenga un `PriceStep` válido; de lo contrario, las distancias basadas en pips se omiten.
