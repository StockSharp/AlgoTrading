# Estrategia Nevalyashka Martingale
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Nevalyashka Martingale es una portación directa del asesor experto de MetaTrader 5 "Nevalyashka3_1". Ejecuta un martingale de símbolo único que alterna entre comprar y vender después de operaciones con pérdida. La estrategia siempre comienza vendiendo y mide el capital de la cuenta para decidir si el ciclo de operaciones anterior terminó en ganancia o pérdida. Una ganancia reinicia el volumen al tamaño de lote base y mantiene la dirección sin cambios, mientras que una pérdida multiplica el tamaño del lote y cambia la dirección en un intento de recuperar la caída.

## Cómo funciona
- **Operación inicial** – se abre una posición corta en el primer candle completado usando el tamaño de lote base.
- **Seguimiento de capital** – la estrategia almacena el valor de capital máximo observado. Cuando no hay posición abierta compara el capital actual con el pico almacenado.
  - Si el capital marcó un nuevo máximo, la siguiente operación usa el tamaño de lote base y repite la última dirección.
  - Si el capital no marcó un nuevo máximo, la siguiente operación aumenta el lote por el multiplicador y cambia de dirección.
- **Stop loss / take profit** – cada orden usa distancias fijas definidas en "puntos" (pasos del instrumento). El take profit refleja el experto original: el stop está `StopLossPoints` alejado de la entrada y el objetivo está `TakeProfitPoints` alejado.
- **Trailing** – una vez que el precio se mueve en `MoveProfitPoints`, el stop se ajusta. Cada movimiento requiere un buffer adicional de `MoveStepPoints` para que el stop solo avance cuando el mercado continúa empujando. Cuando el stop se lleva más allá del precio de entrada, el volumen planificado se divide por el multiplicador, reduciendo la siguiente operación hacia el lote base.
- **Salida de posición** – la posición se cierra inmediatamente cuando el máximo/mínimo del candle alcanza los niveles de stop o take. Después del cierre, la estrategia evalúa el capital y prepara la siguiente señal.

## Parámetros
- `BaseVolume` – tamaño de lote para la operación inicial y cualquier ciclo rentable (predeterminado 0.1).
- `VolumeMultiplier` – factor aplicado después de una pérdida para aumentar el siguiente lote (predeterminado 1.1).
- `TakeProfitPoints` – distancia de take-profit medida en puntos de precio (predeterminado 94).
- `MoveProfitPoints` – excursión favorable mínima antes de que se active el stop trailing (predeterminado 25).
- `MoveStepPoints` – buffer extra requerido entre ajustes de trailing sucesivos (predeterminado 11).
- `StopLossPoints` – distancia inicial del stop-loss medida en puntos de precio (predeterminado 70).
- `CandleType` – marco temporal usado para la gestión de operaciones. El predeterminado son velas de 5 minutos.

## Detalles de gestión de posición
- La estrategia mantiene `_plannedVolume` para reflejar la variable "Lot" original. Solo cambia después de cerrar una operación o cuando el stop supera el break-even.
- `AdjustVolume` respeta las reglas de exchange alineando el tamaño del lote a `VolumeStep` y haciendo cumplir `MinVolume`/`MaxVolume`.
- `GetPointValue` replica la lógica de "punto ajustado" de MT5: para instrumentos cotizados con 3 o 5 decimales, el tamaño del punto se multiplica por 10 para trabajar con pips enteros.
- `HandleLongPosition` y `HandleShortPosition` usan máximos y mínimos de velas para emular la modificación de stops y comportamiento de salida de MT5 sin depender del historial de indicadores.

## Notas de uso
- La estrategia asume que opera con un único instrumento. Agréguela a la estrategia y configure `Security`/`Portfolio` antes de iniciar.
- Dado que es un martingale, el riesgo crece rápidamente tras una serie de pérdidas. Ajuste `BaseVolume` y `VolumeMultiplier` cuidadosamente y pruebe con requisitos de margen realistas.
- Las distancias de stop y take-profit se definen en puntos del instrumento. Asegúrese de que los metadatos del instrumento (`PriceStep`, `VolumeStep`, `MinVolume`) estén completos para que los desplazamientos y cálculos de lote coincidan con su broker.
- La lógica de trailing actúa sobre velas terminadas. Los golpes de stop intrabar pueden ocurrir antes en operativa real dependiendo del camino del precio.
