# Estrategia Executor AO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Executor AO es una estrategia de saucer (platillo) basada en el Awesome Oscillator, originalmente distribuida como el experto de
MetaTrader "Executor AO". El puerto a StockSharp conserva la detección de reversión basada en indicadores mientras simplifica la
gestión del dinero a un tamaño de orden fijo. La estrategia observa las velas completadas del marco temporal configurado, evalúa el
cambio de pendiente del Awesome Oscillator, y abre una sola posición neta cuando ocurren condiciones alcistas o bajistas por debajo
o por encima de la línea cero. El stop de protección opcional, el take-profit y la lógica de trailing reproducen el comportamiento
de gestión de riesgo del EA original.

## Lógica de trading
1. Suscribirse a la serie de velas definida por `CandleType` y alimentar cada vela cerrada al Awesome Oscillator con los parámetros
   `AoShortPeriod` y `AoLongPeriod` configurados.
2. Almacenar los tres últimos valores completados del Awesome Oscillator para reproducir el patrón de acceso al búfer de MetaTrader
   utilizado por el experto original.
3. Cuando no hay posición abierta:
   - **Configuración alcista**: el último valor de AO es mayor que el anterior, el valor anterior es menor que el valor hace dos
     barras (un mínimo), y el último valor permanece por debajo de `-MinimumAoIndent`. En ese caso, enviar una orden de compra a
     mercado con `TradeVolume` lotes.
   - **Configuración bajista**: el último valor de AO es menor que el anterior, el valor anterior es mayor que el valor hace dos
     barras (un máximo), y el último valor permanece por encima de `MinimumAoIndent`. En ese caso, enviar una orden de venta a
     mercado con el volumen fijo.
4. Cuando existe una posición, la estrategia emula las salidas del EA:
   - Calcular los precios de stop-loss y take-profit desde la entrada usando `StopLossPips` y `TakeProfitPips` multiplicados por
     el tamaño de pip ajustado (se replica el manejo de 3/5 dígitos de MetaTrader).
   - Aplicar la regla de trailing stop cuando el precio se mueve a favor de la posición más de `TrailingStopPips +
     TrailingStepPips` pips. El stop solo se avanza si el nuevo nivel está más allá del anterior, coincidiendo con el requisito
     de paso de trailing del EA.
   - Cerrar posiciones largas cuando el precio toca el take-profit o stop-loss, o cuando el valor del Awesome Oscillator de la
     barra anterior se vuelve positivo. Cerrar posiciones cortas cuando el precio alcanza sus objetivos o el valor anterior de AO
     cae por debajo de cero.
5. Todas las órdenes son a mercado; el modelo de posición neta de StockSharp asegura que solo una dirección esté activa a la vez.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Velas de 5 minutos | Marco temporal principal utilizado para calcular y operar la estrategia. |
| `TradeVolume` | `decimal` | `1` | Tamaño de orden fijo utilizado para cada entrada. |
| `AoShortPeriod` | `int` | `5` | Período rápido para la SMA corta del Awesome Oscillator. |
| `AoLongPeriod` | `int` | `34` | Período lento para la SMA larga del Awesome Oscillator. |
| `MinimumAoIndent` | `decimal` | `0.001` | Distancia absoluta mínima desde cero requerida para nuevas señales. Previene operaciones cuando AO ronda el cero. |
| `StopLossPips` | `decimal` | `50` | Distancia de stop-loss protector expresada en pips estilo MetaTrader. Poner en `0` para deshabilitar el stop. |
| `TakeProfitPips` | `decimal` | `50` | Distancia de take-profit expresada en pips. Poner en `0` para deshabilitar el objetivo. |
| `TrailingStopPips` | `decimal` | `5` | Distancia de activación del trailing stop. Solo se usa cuando es mayor que cero. |
| `TrailingStepPips` | `decimal` | `5` | Mejora mínima de precio requerida antes de actualizar el trailing stop. Debe permanecer positivo cuando el trailing está habilitado. |

## Diferencias respecto al EA de MetaTrader
- La versión de MetaTrader permitía el dimensionamiento de posiciones basado en riesgo. El puerto a StockSharp implementa la opción
  de lote fijo (`TradeVolume`) y deja fuera la gestión por porcentaje de riesgo por claridad.
- La gestión de órdenes se simula dentro de la estrategia: cuando los umbrales de stop-loss o take-profit se alcanzan en velas
  completadas, la estrategia envía órdenes a mercado para aplanar la posición. Esto refleja el comportamiento del EA sin crear
  órdenes hijo separadas.
- Los ajustes de trailing ocurren en eventos de cierre de vela en lugar de en cada tick. Esto mantiene la implementación coherente
  con la API de alto nivel mientras sigue la misma lógica de umbral.
- Todas las rutas de código se basan en el patrón `SubscribeCandles` + `Bind` de alto nivel de StockSharp en lugar de copiar
  manualmente los búferes del indicador.

## Consejos de uso
- Alinear `TradeVolume` con el paso de lote del instrumento antes de iniciar la estrategia. El constructor también asigna el mismo
  valor a `Strategy.Volume`, por lo que los métodos auxiliares usan automáticamente el tamaño elegido.
- `MinimumAoIndent` puede aumentarse en mercados ruidosos para evitar cambios frecuentes cerca de cero. Establecerlo en `0`
  reproduce el comportamiento más agresivo del EA.
- Al habilitar el trailing stop, mantener `TrailingStepPips` por encima de cero; de lo contrario el constructor lanza una excepción,
  reproduciendo la validación de parámetros del EA original.
- Adjuntar la estrategia a un gráfico para visualizar tanto las velas como el Awesome Oscillator superpuesto. Esto ayuda a validar
  la detección de mínimos/máximos después de la conversión.

## Indicador
- **Awesome Oscillator**: diferencia entre una media móvil simple rápida y una lenta del precio mediano. La configuración
  predeterminada 5/34 coincide con el indicador de MetaTrader.
