# Estrategia del corredor de rebote semanal
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Corredor de Rebote Semanal replica el comportamiento del MetaTrader 4 Asesor Experto `2_Otkat_Sys_v1_1`. El sistema busca una brecha fuerte entre el cierre de la sesión anterior y el precio de apertura que ocurrió 24 velas antes. Cuando la brecha detectada excede un umbral de corredor configurable y es el día de negociación especificado de la semana, la estrategia ingresa al mercado durante los primeros minutos del nuevo día de negociación. Se aplican niveles protectores de stop-loss y take-profit, y todas las posiciones abiertas se cierran a la fuerza poco antes de que finalice la sesión de negociación.

## Lógica de trading
1. **Preparación de datos**
   - Utiliza velas diminutas por defecto. El tipo de vela se puede configurar para adaptarse a otros tamaños de barra.
   - Realiza un seguimiento del cierre de la vela anterior y mantiene un buffer circular que devuelve el precio de apertura observado hace 24 velas.
2. **Generación de señal**
   - En el día de negociación especificado de la semana (MetaTrader formato: `0 = Sunday`, `6 = Saturday`), la estrategia evalúa las velas terminadas cuya hora local está entre las 00:00 y las 00:03.
   - Calcula la diferencia entre la apertura histórica (hace 24 velas) y la última vela cerrada. Si la diferencia excede el umbral del corredor configurado, se envía una orden de mercado:
     - **Configuración larga**: La apertura histórica menos el cierre anterior es mayor que el umbral del corredor.
     - **Configuración breve**: el cierre anterior menos la apertura histórica es mayor que el umbral del corredor.
   - Cada día de negociación puede activar como máximo una entrada.
3. **Gestión comercial**
   - Los niveles de stop-loss y take-profit se expresan en puntos. El tamaño del tick del instrumento convierte los valores en puntos en compensaciones de precios reales.
   - Las operaciones largas añaden la compensación MT4 original de tres puntos extra a la distancia de obtención de beneficios.
   - La estrategia monitorea continuamente los máximos y mínimos de las velas para detectar topes de pérdidas o toma de ganancias y cierra la posición abierta con una orden de mercado cuando se activa.
   - Cualquier posición abierta restante se cierra después de las 22:45 hora de cambio local para emular la regla fija de final del día del Asesor Experto original.

## Parámetros
| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `TakeProfitPoints` | Distancia de toma de ganancias en puntos. Las operaciones largas añaden tres puntos adicionales, como se define en el script MT4. | `5` |
| `StopLossPoints` | Distancia de stop-loss en puntos. | `49` |
| `TradeVolume` | Volumen presentado con órdenes de mercado. El valor se alinea automáticamente con el paso de volumen del instrumento. | `1` |
| `CorridorPoints` | Brecha mínima requerida entre la apertura histórica y el cierre más reciente. | `10` |
| `TradeDayOfWeek` | Día de negociación en numeración MetaTrader (`0 = Sunday`… `6 = Saturday`). | `5` (viernes) |
| `CandleType` | Tipo de datos de vela utilizado para el análisis. | `1 minute` |

## Notas
- La estrategia opera exclusivamente con velas completadas para alinearse con las pautas del proyecto.
- Asegúrese de que el instrumento seleccionado proporcione suficientes datos históricos para crear el búfer de 24 velas antes de esperar entradas.
- Los parámetros basados en volumen y puntos deben ajustarse para que coincidan con la especificación del instrumento (tamaño del tick, paso del lote, cronograma de negociación).
