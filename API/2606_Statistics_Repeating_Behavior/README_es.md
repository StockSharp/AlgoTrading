# Estrategia de Estadística de Comportamiento Repetido
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Estrategia intradía que estudia cómo se comportaron las velas a la misma hora del día durante las últimas N sesiones de trading. Para cada nueva barra compara los tamaños acumulados de cuerpos alcistas y bajistas de días anteriores. Si la presión alcista domina, abre una posición larga al cierre de la barra; de lo contrario, va en corto. Las posiciones se cierran en la siguiente barra y un stop loss fijo en pips imita la lógica original de MetaTrader. El tamaño de la posición sigue un martingale de proporción áurea, creciendo tras pérdidas y reiniciándose tras ganancias.

## Lógica de trading

1. Al inicio de cada nueva vela, cerrar cualquier posición abierta de la barra anterior.
2. Buscar velas de los últimos `HistoryDays` días de trading que abrieron a la misma hora y minuto.
3. Sumar los cuerpos de las velas (en puntos) por separado para cierres alcistas y bajistas, ignorando cuerpos menores que `MinimumBodyPoints`.
4. Si la suma alcista supera la bajista → abrir una posición larga con el volumen actual.
5. Si la suma bajista supera la alcista → abrir una posición corta.
6. Aplicar un stop loss de `StopLossPips` convertido a través del paso mínimo de precio del instrumento. El stop se comprueba contra los extremos intrabarra cuando la vela finaliza.
7. Cuando se cierra la operación:
   - Si el resultado es rentable, restablecer el volumen a `InitialVolume`.
   - De lo contrario, multiplicar el volumen actual por `MartingaleFactor` (respetando el paso de volumen y los límites).

## Parámetros

- **HistoryDays** *(predeterminado: 10)* — número de días anteriores a incluir en las estadísticas.
- **MinimumBodyPoints** *(predeterminado: 10)* — las velas con un cuerpo menor a este umbral (en puntos) se ignoran.
- **StopLossPips** *(predeterminado: 15)* — distancia en pips del stop de protección.
- **InitialVolume** *(predeterminado: 0.1)* — tamaño inicial de la orden antes de ajustes por martingale.
- **MartingaleFactor** *(predeterminado: 1.618)* — multiplicador aplicado tras una operación perdedora.
- **CandleType** *(predeterminado: 1 hora)* — marco temporal utilizado para las velas.

## Características de trading

- **Lado del mercado**: Ambos, largo y corto, dependiendo de las estadísticas.
- **Marco temporal**: Configurable (horario por defecto) con coincidencia exacta por hora y minuto.
- **Gestión de posición**: Una sola posición a la vez, cerrada en la siguiente barra o cuando se activa el stop loss.
- **Riesgo**: Usa stop fijo en pips y sizing por martingale, que puede aumentar el volumen rápidamente tras pérdidas consecutivas.
- **Instrumentos**: Funciona con instrumentos que proporcionan un `MinPriceStep` válido y límites de volumen.

## Notas de implementación

- Los cuerpos de las velas se almacenan por minuto del día en una cola deslizante limitada por `HistoryDays`.
- Los volúmenes se normalizan al paso de volumen del instrumento y están acotados por `MinVolume`/`MaxVolume`.
- La detección del stop loss depende de los extremos de la vela completada para emular la ejecución intrabarra del experto MQL5 original.
- Todos los comentarios de código en línea están en inglés para cumplir con los requisitos del repositorio.
