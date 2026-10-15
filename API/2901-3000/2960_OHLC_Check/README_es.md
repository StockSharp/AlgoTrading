# Estrategia de Verificación OHLC
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Verificación OHLC replica el clásico asesor experto de MetaTrader que inspecciona la estructura de apertura, máximo, mínimo y cierre de la vela anterior. La estrategia evalúa el cuerpo de la vela en un desplazamiento histórico configurable y abre una nueva posición en la dirección de ese cuerpo con la posibilidad de invertir la señal. Está diseñada para una ejecución simple basada en la acción del precio sin depender de osciladores o medias móviles.

## Lógica de trading
1. La estrategia se suscribe a la serie de velas configurada y espera a que la barra finalice antes de procesar.
2. Para cada vela finalizada, el motor almacena el precio de apertura y cierre para que el desplazamiento definido por el usuario ("SignalShift") pueda referenciar barras más antiguas.
3. Un cuerpo alcista (cierre por encima de la apertura) genera una señal larga, mientras que un cuerpo bajista (cierre por debajo de la apertura) genera una señal corta. Si el cuerpo es plano no se crea ninguna operación.
4. El indicador `ReverseSignals` puede invertir la dirección de la operación, reproduciendo el modo de trading inverso del asesor experto original.
5. Cuando no existe una posición activa, la estrategia intenta abrir una orden de mercado en la dirección detectada siempre que el diferencial actual esté dentro del umbral permitido de `SpreadLimitPips`. El diferencial se monitorea usando el flujo del libro de órdenes.
6. Cuando ya existe una posición, la señal opuesta activa el cierre de la posición en lugar de una reversión completa, siguiendo la lógica MQL.
7. Las protecciones opcionales de stop-loss y take-profit se inician al arrancar usando distancias en pips convertidas al paso de precio del instrumento, recreando el comportamiento de gestión del dinero MQL.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `CandleType` | Marco temporal de 5 minutos | Serie de datos utilizada para la evaluación OHLC. |
| `StopLossPips` | 50 | Distancia del stop-loss medida en pips; `0` desactiva el stop. |
| `TakeProfitPips` | 100 | Distancia del take-profit medida en pips; `0` desactiva el objetivo. |
| `ReverseSignals` | `false` | Invierte la dirección de las señales largas y cortas. |
| `SpreadLimitPips` | 1 | Diferencial máximo, en pips, permitido al abrir una nueva posición. |
| `SignalShift` | 1 | Número de velas completadas hacia atrás usadas para el cálculo de la señal (1 = vela anterior). |
| `OrderVolume` | 1 | Volumen enviado con cada orden de mercado. |

## Notas de uso
- La estrategia utiliza los metadatos del instrumento para convertir valores en pips a distancias de paso de precio. Los instrumentos con 3 o 5 decimales reciben automáticamente el ajuste estándar de diez puntos por pip.
- La suscripción al libro de órdenes debe estar habilitada en el feed de datos para que las verificaciones de diferencial funcionen correctamente. Si no hay cotizaciones bid/ask disponibles, la estrategia omitirá la apertura de nuevas operaciones.
- Los stops de protección se inician una vez durante `OnStarted`. Cambiar los parámetros de stop posteriormente requiere reiniciar la estrategia para aplicar las nuevas protecciones.
- Dado que la estrategia solo reacciona al cuerpo de la vela, los valores de máximo y mínimo se ignoran exactamente como en la versión original de MetaTrader.

## Pasos de despliegue
1. Adjunte la estrategia a un instrumento que suministre tanto velas como cotizaciones del libro de órdenes.
2. Configure los parámetros según el estilo de trading deseado (marco temporal, distancias en pips y volumen).
3. Lance la estrategia. Esperará a la próxima vela completada antes de realizar cualquier acción.
4. Monitoree el registro en busca de rechazos por diferencial o operaciones ejecutadas, y ajuste los parámetros según sea necesario.
