# Estrategia UmnickTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema adaptativo de reversión a la media convertido del asesor experto MQL5 original UmnickTrader. La estrategia trabaja con una única posición a la vez, alternando entre sesgo largo y corto dependiendo del resultado de la operación anterior. Evalúa el movimiento de precio usando el promedio de los precios de apertura, máximo, mínimo y cierre, y solo toma acción una vez que ese promedio se ha desplazado al menos la distancia `StopBase` configurada.

## Lógica principal

- Para cada vela terminada se calcula el precio promedio `(O + H + L + C) / 4`.
- Las señales se procesan solo cuando la diferencia absoluta entre el promedio actual y el promedio procesado anteriormente es mayor o igual a `StopBase`. Esto imita el comportamiento del EA original de esperar un movimiento suficientemente grande.
- Cuando no hay posición abierta la estrategia calcula distancias adaptativas de take-profit y stop-loss usando dos buffers circulares que almacenan las ocho excursiones de ganancia y pérdida más recientes.
- Después de una operación rentable, la excursión favorable máxima observada mientras la posición estaba abierta se guarda en el buffer de ganancia (menos un relleno de spread), mientras que el buffer de pérdida recibe `StopBase + 7 * Spread`.
- Después de una operación perdedora, el buffer de ganancia se restablece a `StopBase - 3 * Spread`, el buffer de pérdida se actualiza con el drawdown registrado más un relleno de spread, y la dirección de trading se invierte para que la próxima configuración opere el lado opuesto.

## Gestión de operaciones

- La distancia predeterminada tanto para el take-profit como para el stop-loss es `StopBase`. Si el buffer de ganancia o pérdida acumulado supera `StopBase / 2`, sus respectivos promedios reemplazan la distancia predeterminada para ampliar o ajustar adaptativamente los niveles de salida.
- Se usan órdenes de mercado para las entradas. Los precios esperados de take-profit y stop-loss se almacenan y gestionan por la propia estrategia, por lo que las posiciones se cierran cuando los máximos o mínimos de la vela tocan los niveles correspondientes.
- Mientras una posición está activa, el movimiento favorable más alto y el drawdown más profundo se rastrean usando extremos intrabar. Estas estadísticas alimentan los buffers cuando la operación se cierra.
- Solo puede existir una posición en cualquier momento. Una nueva señal se ignora si la operación anterior no se ha completado.

## Parámetros

- `StopBase` – distancia base (en unidades de precio) requerida para tratar un movimiento como significativo y la distancia TP/SL predeterminada. Predeterminado: `0.017`.
- `TradeVolume` – volumen para órdenes de mercado. Predeterminado: `0.1`.
- `Spread` – compensación de spread aplicada al actualizar los buffers adaptativos. Predeterminado: `0.0005`.
- `CandleType` – suscripción de velas utilizada para evaluar promedios. Predeterminado: `TimeSpan.FromMinutes(5).TimeFrame()`.

## Clasificación y filtros

- **Dirección**: Ambos (pero nunca simultáneamente).
- **Estilo**: Swing adaptativo / contratendencia.
- **Indicadores**: Promedio de precio, buffers de excursión personalizados.
- **Stops**: Stop-loss y take-profit dinámico gestionado por la estrategia.
- **Complejidad**: Intermedio – combina buffers con estado con dimensionamiento adaptativo de salida.
- **Marco temporal**: Configurable mediante `CandleType`.
- **Estacionalidad / Filtros de noticias**: No se usan.
- **Gestión de riesgos**: El tamaño de la posición está fijado por `TradeVolume`; las distancias de salida se adaptan según el rendimiento reciente.
