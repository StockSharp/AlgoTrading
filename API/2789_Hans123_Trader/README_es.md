# Estrategia Hans123 Trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Hans123 Trader es un sistema de ruptura convertido desde el asesor experto original de MetaTrader 5 *Hans123_Trader*. La estrategia escanea un rango de precios dinámico y coloca órdenes pendientes de stop durante una ventana intradía configurable. Los stops de protección, objetivos de beneficio y reglas de trailing replican la lógica MQL5 para que el puerto StockSharp se comporte como el robot original.

## Conceptos principales
- **Ruptura de rango** – utiliza el máximo más alto y el mínimo más bajo de las últimas *N* velas para definir el canal de ruptura.
- **Filtro de tiempo** – solo evalúa señales entre las horas de inicio y fin para evitar el ruido nocturno.
- **Órdenes pendientes sincrónicas** – actualiza las órdenes buy stop y sell stop en cada vela completada dentro de la ventana de trading.
- **Control de riesgo** – distancias opcionales de stop-loss, take-profit y trailing stop expresadas en pips.
- **Trailing dinámico** – una vez que el precio recorre la distancia de trailing stop más trailing step, el stop de protección se ajusta para asegurar ganancias.

## Lógica de trading
1. Suscribirse a la serie de velas seleccionada y esperar a que se forme la ventana del indicador `RangeLength`.
2. En cada vela finalizada:
   - Actualizar el canal de máximo/mínimo de 80 barras (configurable).
   - Omitir el procesamiento si el tiempo actual está fuera del intervalo `[StartHour, EndHour)`.
   - Cancelar las órdenes de entrada existentes y colocar nuevas órdenes stop:
     - **Buy stop** en el máximo del rango por `OrderVolume`.
     - **Sell stop** en el mínimo del rango por `OrderVolume`.
3. Cuando se ejecuta una orden de entrada:
   - Cancelar la orden pendiente opuesta.
   - Registrar órdenes de stop-loss y take-profit si las distancias en pips correspondientes son mayores que cero.
4. Mientras haya una posición abierta:
   - Si el precio avanza al menos `TrailingStopPips + TrailingStepPips`, mover el stop de protección hacia el mercado en `TrailingStopPips`.
   - Las órdenes de protección se cancelan automáticamente cuando la posición vuelve a plano.

## Parámetros
| Nombre | Descripción | Valor predeterminado |
| --- | --- | --- |
| `OrderVolume` | Tamaño de la orden para entradas en ruptura. | `0.1` |
| `RangeLength` | Número de velas en el canal de ruptura. | `80` |
| `StopLossPips` | Distancia del stop-loss en pips (0 desactiva el stop). | `50` |
| `TakeProfitPips` | Distancia del take-profit en pips (0 desactiva el objetivo). | `50` |
| `TrailingStopPips` | Distancia del trailing stop en pips (0 desactiva el trailing). | `10` |
| `TrailingStepPips` | Pips adicionales necesarios antes de que se actualice el trailing stop. Debe ser positivo cuando el trailing está habilitado. | `5` |
| `StartHour` | Hora del día inclusiva (UTC) en que comienzan las órdenes de ruptura. | `6` |
| `EndHour` | Hora del día exclusiva (UTC) en que se detienen las órdenes de ruptura. | `10` |
| `CandleType` | Tipo de datos de vela y marco temporal de trabajo. | Velas de `1 hora` |

## Notas prácticas
- El tamaño del pip se adapta a los decimales del instrumento (los símbolos forex de 3/5 dígitos reciben el ajuste habitual *×10*).
- Los trailing stops solo se crean después de que una posición recorre la distancia de activación; si `StopLossPips` es cero, el stop inicial se omite hasta que se cumplan las condiciones de trailing.
- Mantenga los permisos del portafolio alineados con el `OrderVolume` seleccionado y el tamaño del contrato del instrumento.
- La conversión StockSharp usa ayudas gráficas para visualizar velas, el canal y las operaciones para depuración.

## Diferencias con la versión MQL5
- Las órdenes de stop y objetivo se registran a través de los ayudantes de alto nivel de StockSharp en lugar de las solicitudes de trading de MetaTrader.
- Los valores predeterminados de volumen siguen siendo idénticos (0.1 lotes) pero pueden optimizarse mediante metadatos `StrategyParam`.
- Las órdenes pendientes se actualizan en cada vela completada en lugar de esperar actualizaciones a nivel de tick, adaptándose al modelo de eventos de StockSharp.

## Uso
1. Adjuntar la estrategia a un par de portafolio/instrumento y verificar que la suscripción de velas coincida con el marco temporal deseado.
2. Ajustar los parámetros según la volatilidad del instrumento y los límites de sesión.
3. Iniciar la estrategia; monitorear la superposición del área del gráfico para confirmar los niveles de ruptura y las operaciones ejecutadas.
4. Usar los parámetros integrados para optimización dentro del entorno de pruebas de StockSharp si se desea.
