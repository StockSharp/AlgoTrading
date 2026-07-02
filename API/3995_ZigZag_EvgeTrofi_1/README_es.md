# Estrategia ZigZag EvgeTrofi 1
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)


## Descripción general
ZigZag EvgeTrofi 1 reproduce el comportamiento del asesor experto original MetaTrader que reacciona al último punto de giro de ZigZag. La estrategia monitorea cada vela completa, identifica el pivote ZigZag más reciente utilizando la configuración clásica de profundidad, desviación y retroceso, e ingresa al mercado si el pivote aún es reciente. Una oscilación alta desencadena una posición larga, mientras que una oscilación baja abre una posición corta, coincidiendo con el mapa de señales original EA.

## Lógica de trading
- Suscríbase al tipo de vela configurado y proporcione indicadores más altos/más bajos cuya longitud coincida con el parámetro de profundidad del ZigZag. El par de indicadores emulan la detección nativa de oscilación en ZigZag sin depender de buffers personalizados.
- Cuando se cierra una vela, verifique si su máximo toca el máximo rastreado o su mínimo toca el mínimo rastreado. Sólo cambie a un nuevo pivote si se cumple la desviación requerida en los pasos de precio y se respeta la distancia de retroceso (barras mínimas entre pivotes opuestos).
- Una vez que se registra un pivote, sigue contando cuántas barras han pasado. El parámetro de urgencia define cuántas barras después del pivote todavía se consideran procesables. Las señales anteriores a este límite se ignoran, lo que evita entradas tardías.
- Para un pivote alto la estrategia se prepara para comprar y para un pivote bajo se prepara para vender. Si una posición abierta ya coincide con la dirección prevista, la señal se marca como manejada y no se envían órdenes adicionales.
- Si la cuenta actualmente tiene exposición en la dirección opuesta, la estrategia envía una orden de mercado para aplanarse antes de abrir una nueva operación. Posteriormente envía inmediatamente una orden de mercado con el volumen configurado para establecer la nueva posición.
- Cada acción requiere un estado del indicador completamente formado, una vela terminada y un volumen de operaciones positivo. La estrategia verifica la conectividad y los permisos usando `IsFormedAndOnlineAndAllowTrading()` antes de interactuar con el mercado, asegurando que las órdenes solo se envíen en condiciones comerciales saludables.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Depth` | Profundidad de ZigZag que define la ventana de detección de swing. | 17 |
| `Deviation` | Movimiento de precio mínimo en puntos requerido para confirmar el pivote del mismo tipo. Convertido internamente a pasos de precio de instrumentos. | 7 |
| `Backstep` | Número mínimo de barras que deben pasar antes de cambiar a un pivote opuesto. | 5 |
| `Urgency` | Número máximo de barras después de un pivote durante las cuales se permiten operaciones. | 2 |
| `Candle Type` | Tipo de datos de vela (período de tiempo o agregación personalizada) utilizado para los cálculos. | marco de tiempo de 5 minutos |
| `Volume` | Volumen de órdenes de mercado enviado en cada entrada. | 0.1 |

## Notas de implementación
- Los indicadores más alto/más bajo están vinculados a través del nivel alto `SubscribeCandles().Bind()` API, por lo que la estrategia opera solo en las velas finales y evita el almacenamiento en búfer manual.
- El parámetro de desviación se transforma en una diferencia de precio absoluta utilizando el paso de precio del instrumento. Si el símbolo carece de metadatos de paso de precio, se utiliza un valor de 1 como alternativa, manteniendo la lógica coherente en todos los intercambios.
- Una guardia booleana evita intercambios duplicados por pivote, coincidiendo con el comportamiento MetaTrader EA que solo actúa una vez por swing.
- La integración de gráficos incorporada dibuja velas y ejecuta operaciones automáticamente cuando los gráficos están disponibles, lo que ayuda a validar visualmente los puntos de oscilación y las entradas.
- La gestión de posiciones es simétrica: cualquier exposición opuesta se nivela con una orden de mercado de igual volumen antes de establecer la nueva operación, manteniendo la cartera unilateral como el asesor experto original.
