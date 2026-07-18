# Estrategia de ruptura de FT TIME BIGDOG
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia **FT TIME BIGDOG** es un sistema de ruptura de sesión de Londres convertido del MetaTrader 4 asesores expertos `FT_TIME_BIGDOG.mq4` (directorio `MQL/9259`).
Mide el rango de consolidación que se forma entre las horas de inicio y finalización configuradas y luego coloca órdenes de detención por encima y por debajo de ese rango una vez que se cierra la ventana.
La versión StockSharp mantiene el comportamiento original al tiempo que expone parámetros configurables para el tiempo de ruptura, la distancia de las órdenes y la gestión de riesgos.

## Lógica de trading
1. Cada día de negociación, la estrategia registra el máximo más alto y el mínimo más bajo de las velas terminadas cuya hora de apertura se encuentra entre **StartHour** y **StopHour** (inclusive).
2. Después de que finalice la vela de la hora de parada, si el rango acumulado es menor que **RangeLimitPoints**, dos órdenes de parada pendientes se vuelven elegibles:
   - Una **parada de compra** en el máximo registrado.
   - Una **parada de venta** en el mínimo registrado.
3. Las órdenes se crean solo si el precio de mercado está al menos **OrderBufferPoints** lejos del nivel de entrada. Los mejores precios de oferta/demanda se utilizan cuando están disponibles; de lo contrario, se utiliza el último cierre de vela.
4. Cada orden pendiente incluye un stop de protección en el lado opuesto del rango y una compensación de toma de ganancias definida por **TakeProfitPoints**.
5. Cuando se abre una posición, se cancela la orden pendiente opuesta. La posición activa se monitorea en velas terminadas: si el precio toca el nivel de stop loss almacenado o de toma de ganancias, la posición se cierra en el mercado.
6. El ciclo se ejecuta como máximo una vez al día; Todo el estado se restablece al comienzo del siguiente día de negociación.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `StartHour` | 14 | Hora (0–23) que marca el inicio de la ventana de acumulación. |
| `StopHour` | 16 | Hora en la que los pedidos pendientes se vuelven elegibles. Debe ser mayor o igual a `StartHour`. |
| `RangeLimitPoints` | 50 | Ancho máximo del rango de sesión en puntos del corredor (puntos × `PointMultiplier`). No se realizan pedidos si el rango es más amplio. |
| `TakeProfitPoints` | 50 | Distancia de obtención de beneficios aplicada a las posiciones activadas, expresada en puntos del corredor. |
| `OrderBufferPoints` | 20 | Distancia mínima requerida entre el precio de mercado y una orden pendiente. Evita que los pedidos se realicen demasiado cerca del precio actual. |
| `PointMultiplier` | 1 | Multiplicador aplicado al tamaño del punto del instrumento. Establezca en 10 para símbolos de divisas de cinco dígitos. |
| `Volume` | 0.1 | Volumen de órdenes para ambas órdenes stop. |
| `CandleType` | 1 hora | Serie de velas utilizadas para medir el alcance y evaluar la señal de accionamiento. |

## Gestión de riesgos y comercio
- El stop loss para operaciones largas equivale al mínimo de la sesión; el stop loss para operaciones cortas es igual al máximo de la sesión.
- Los niveles de toma de ganancias se calculan a partir del precio de ruptura usando `TakeProfitPoints` y el tamaño en puntos del instrumento.
- Todos los controles de riesgo se ejecutan en los eventos de cierre de velas; Las excursiones dentro de la barra más allá de los niveles de parada pueden provocar salidas retrasadas.

## Diferencias vs. Asesor Experto Original
- La versión MetaTrader opera en eventos de tick, mientras que este puerto depende de velas terminadas y actualizaciones de nivel 1. Por lo tanto, el comportamiento dentro de una vela puede variar ligeramente.
- La conversión de puntos utiliza `Security.PriceStep` multiplicado por `PointMultiplier`. Verifique esta combinación antes de ejecutar en vivo.
- Los pedidos se realizan únicamente cuando `StartHour <= StopHour`. Este puerto no admite ventanas de medianoche.

## Notas de uso
1. Asigne la seguridad deseada y verifique que los datos de nivel 1 estén disponibles para realizar comprobaciones precisas del buffer.
2. Configure el horario de negociación según la zona horaria del corredor.
3. Primero ejecute la simulación para validar la conversión de puntos y el tiempo en relación con su fuente de datos.
4. Restablezca o detenga la estrategia antes de modificar manualmente las órdenes pendientes para evitar estados conflictivos.

## Archivos
- `CS/FtTimeBigdogStrategy.cs`: implementación principal de StockSharp con comentarios detallados en línea.
- `MQL/9259/FT_TIME_BIGDOG.mq4`: fuente original MetaTrader utilizada para la conversión.
