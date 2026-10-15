# Estrategia de Lista de Posiciones
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Lista de Posiciones** reproduce el comportamiento del script original de MetaTrader imprimiendo periódicamente las posiciones actuales del portafolio en el log de la estrategia. Es un helper de solo monitoreo que nunca coloca órdenes. En cambio, construye un snapshot de las posiciones abiertas para que el operador pueda inspeccionar símbolo, dirección, tamaño, precio de entrada y ganancia actual directamente desde Designer o los logs de StockSharp.

## Características clave
- Reporte de posiciones impulsado por temporizador con el primer snapshot entregado inmediatamente después de que la estrategia comienza.
- Filtrado opcional por el instrumento de la estrategia o por identificador de estrategia (el análogo del número mágico de MetaTrader).
- Salida de log detallada que incluye el identificador de posición, último tiempo de cambio, lado, cantidad, precio promedio y ganancia.
- Procesamiento seguro para subprocesos que evita superposiciones de callbacks del temporizador cuando el entorno está ocupado.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ------ | ----------- | -------------- |
| `StrategyIdFilter` | Identificador de estrategia a omitir. Cuando se deja vacío, se reportan todas las posiciones. | Cadena vacía |
| `SelectionMode` | Controla si se reportan posiciones de cada símbolo o solo de `Strategy.Security`. | `AllSymbols` |
| `TimerInterval` | Intervalo entre snapshots consecutivos de posiciones. | 6 segundos |

## Cómo funciona
1. Durante `OnStarted`, la estrategia verifica que haya un portafolio adjunto y que el intervalo del temporizador sea positivo.
2. Se crea un `System.Threading.Timer` con retraso cero para que el primer reporte se produzca inmediatamente y luego se repita en el intervalo configurado.
3. Cada tick del temporizador llama a `ProcessPositions`, que itera sobre `Portfolio.Positions`, aplica los filtros opcionales de símbolo e identificador de estrategia, y adjunta líneas formateadas a un `StringBuilder`.
4. Cuando al menos una posición pasa los filtros, la tabla ensamblada se escribe en el log con `LogInfo`. Si nada coincide, en su lugar se registra una notificación concisa.
5. Las superposiciones del temporizador se previenen con un guarda interlocked para que la I/O lenta no pueda desencadenar ejecuciones concurrentes.

## Notas de uso
- Asigne tanto `Portfolio` como `Connector` antes de iniciar la estrategia. Si `SelectionMode` está configurado en `CurrentSymbol`, también configure `Strategy.Security` al instrumento que desea monitorear.
- Para emular el filtro `magic` de MetaTrader, llene `StrategyIdFilter` con el valor de cadena usado como `StrategyId` cuando otras estrategias envían órdenes. Esas posiciones se excluirán del reporte.
- La estrategia nunca modifica posiciones ni registra órdenes, lo que la hace segura para ejecutar junto con lógica de trading en vivo como un widget informacional.
- La salida del log se agrupa bajo el encabezado de columna `Idx | Symbol | PositionId | LastChange | Side | Quantity | AvgPrice | PnL` para que pueda ser fácilmente analizado por herramientas externas si es necesario.

## Diferencias con la versión MQL
- MetaTrader usa un número `magic` de 64 bits sin signo. Las posiciones de StockSharp exponen el identificador de estrategia como una cadena, por lo tanto el filtro acepta valores textuales.
- En lugar de escribir en el comentario del gráfico, este port registra el snapshot mediante `LogInfo`, que es visible en Designer, Runner o cualquier listener de log.
- La versión de StockSharp protege contra invocaciones solapadas del temporizador para mantenerse responsiva bajo carga pesada.
- Las marcas de tiempo dependen de `Position.LastChangeTime`, que refleja las actualizaciones de posición de StockSharp, mientras que el script MQL mostraba el tiempo de creación del ticket.
