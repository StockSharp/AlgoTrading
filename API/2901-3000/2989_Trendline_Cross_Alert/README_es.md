# Estrategia de Alerta de Cruce de Línea de Tendencia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el comportamiento del asesor experto de MetaTrader original que observaba los cruces de precio de líneas horizontales y líneas de tendencia dibujadas manualmente. Monitorea continuamente las velas finalizadas, comprueba si el cuerpo de la vela cruzó algún nivel registrado y genera alertas la primera vez que se produce un cruce. No se envían órdenes automáticas por defecto; el módulo se enfoca en rastrear niveles discrecionales e informar al operador.

## Aspectos destacados de la conversión
- Solo se consideran las líneas etiquetadas con el valor de *Color de monitoreo*, reflejando el EA original que filtraba objetos por color.
- Una vez que se detecta un cruce, la línea se marca internamente para que las velas posteriores no disparen alertas duplicadas. Esto refleja la recoloración del objeto al color `CrossedColor` en MetaTrader.
- Debido a que StockSharp no expone objetos de gráfico desde el terminal, los niveles se definen a través de parámetros de texto. Las entradas horizontales se analizan desde bloques `Name|Color|Price`, mientras que las líneas de tendencia usan `Name|Color|StartTime|StartPrice|EndTime|EndPrice` y se evalúan como líneas infinitas entre los dos puntos de anclaje.
- Las opciones de alerta, notificación push y correo electrónico se mapean a entradas de registro informativas para que el flujo de trabajo permanezca transparente incluso sin canales de notificación específicos de la plataforma.

## Parámetros
| Parámetro | Tipo | Descripción |
| --- | --- | --- |
| `MonitoringColor` | `string` | Etiqueta de color que las líneas deben coincidir para ser monitoreadas. No distingue mayúsculas/minúsculas. |
| `CrossedColor` | `string` | Etiqueta utilizada en los mensajes de alerta para indicar que la línea fue cruzada. |
| `HorizontalLevelsInput` | `string` | Lista de niveles horizontales separados por punto y coma. Cada entrada es `Name|Color|Price`; si el color se omite, se asume el color de monitoreo. |
| `TrendlineDefinitions` | `string` | Lista de líneas de tendencia separadas por punto y coma. Cada entrada es `Name|Color|StartTime|StartPrice|EndTime|EndPrice`. Los tiempos deben estar en formato ISO 8601 y usar la zona horaria del calendario de trading. |
| `EnableAlerts` | `bool` | Cuando es `true`, la estrategia escribe una entrada de registro informativa que describe el cruce. |
| `EnableNotifications` | `bool` | Añade una segunda entrada de registro que emula una notificación push. |
| `EnableEmails` | `bool` | Añade una tercera entrada de registro que emula una alerta por correo electrónico. |
| `CandleType` | `DataType` | Serie de velas utilizada para monitorear el mercado. |

## Formato de definición
1. Separar múltiples entradas con punto y coma (`;`).
2. Los niveles horizontales pueden omitir el nombre o el color:
   - `1.1050` → monitoreado como `Horizontal 1` al precio `1.1050` usando el color de monitoreo.
   - `Resistance|1.1180` → nombre personalizado aún usando el color de monitoreo.
   - `Breakout|Blue|1.1225` → el color personalizado aún debe coincidir con `MonitoringColor` para ser rastreado.
3. Las líneas de tendencia requieren dos puntos de anclaje con marcas de tiempo ISO 8601 (`2024-03-15T10:00:00Z`). Los valores de color faltantes se predeterminan al color de monitoreo. Las líneas se extrapolan más allá de los anclajes exactamente como las líneas de tendencia de MetaTrader.

## Flujo de ejecución
1. Durante `OnStarted` las definiciones de texto se analizan en estructuras de tipo fuerte y se almacenan en memoria.
2. Las velas finalizadas de la suscripción configurada activan `ProcessCandle`.
3. El método comprueba si la vela abrió en un lado de un nivel y cerró en el otro. Si es así, la línea se marca como cruzada y se genera un mensaje.
4. Los mensajes incluyen la dirección del cruce, el precio teórico de la línea y el precio de cierre para que los traders discrecionales puedan reaccionar manualmente.

## Notificaciones
Las estrategias de StockSharp emiten mensajes de registro en lugar de ventanas emergentes de UI. Cada canal de notificación habilitado produce una entrada de registro separada, lo que permite a la aplicación anfitriona enrutarlas a sistemas de alerta reales si es necesario.

## Lista de verificación de uso
1. Seleccionar el instrumento y el marco temporal, luego configurar `CandleType` en consecuencia.
2. Llenar `HorizontalLevelsInput` y `TrendlineDefinitions` con las líneas dibujadas en tu espacio de trabajo de MetaTrader (o cualquier valor personalizado).
3. Ajustar los booleanos de notificación para que coincidan con los canales de alerta deseados.
4. Iniciar la estrategia. El subsistema de gráficos puede usarse para trazar líneas manualmente si se desea; este módulo se centra en la detección.

## Configuración de ejemplo
```
MonitoringColor = "Yellow"
CrossedColor = "Green"
HorizontalLevelsInput = "DailyPivot|Yellow|1.1025;WeeklyHigh|Yellow|1.1100"
TrendlineDefinitions = "UpperChannel|Yellow|2024-03-14T08:00:00Z|1.0950|2024-03-14T16:00:00Z|1.1080"
EnableAlerts = true
EnableNotifications = true
EnableEmails = false
CandleType = 15 minute candles
```
Esta configuración monitorea dos niveles estáticos y una línea de tendencia ascendente. Un mensaje como `Price crossed horizontal line 'DailyPivot' upward at 1.10250 ...` se escribirá la primera vez que un cierre pase a través de cada nivel.

## Gestión de riesgos y extensiones
- La estrategia no modifica posiciones. Combínala con lógica de ejecución separada si se requiere trading automático.
- Para restablecer alertas, detener y reiniciar la estrategia o ajustar las cadenas de definición. Persista el estado de `HashSet` se evita intencionalmente para permanecer cerca del comportamiento original del EA.
- Se pueden superponer salvaguardas adicionales como filtros de sesión o comprobaciones de volatilidad extendiendo el método `ProcessCandle`.
