# Estrategia de Observación de Mercado en Tick
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Observación de Mercado en Tick** replica el comportamiento del script de MetaTrader `scOnTickMarketWatch.mq5`. El script original escanea continuamente la lista de Market Watch y genera un evento personalizado cuando llega un nuevo tick para cualquier símbolo, imprimiendo el precio de oferta y la información de spread. Este port en C# convierte ese comportamiento en una estrategia de alto nivel de StockSharp que escucha actualizaciones Level1 y registra la información de tick a través del logger de estrategia.

La estrategia es intencionalmente no comercial. Su propósito es proporcionar diagnósticos o monitoreo de datos de tick entrantes en múltiples instrumentos conectados al mismo conector. Dado que se basa en las suscripciones de datos de StockSharp, la solución es impulsada por eventos y no requiere retrasos o bucles manuales como la versión MQL.

## Características principales
- Monitorea el instrumento principal de la estrategia y cualquier instrumento adicional definido en una lista separada por comas.
- Se suscribe a datos Level1 para cada instrumento con el fin de capturar actualizaciones de oferta/demanda.
- Calcula el spread (ask menos bid) cuando ambos lados están disponibles y registra información detallada en inglés.
- Refleja el índice de Market Watch manteniendo un orden interno idéntico a la lista especificada por el usuario.
- Proporciona advertencias claras cuando un símbolo no puede ser resuelto por el `SecurityProvider` configurado.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| ---- | ---- | ------- | ----------- |
| `SymbolsList` | `string` | `""` | Lista separada por comas de identificadores de instrumentos adicionales (por ejemplo, `AAPL@NASDAQ,MSFT@NASDAQ`) que deben observarse además del `Strategy.Security` principal. Cada identificador debe existir en el `SecurityProvider` actual. |

## Cómo funciona
1. Durante `OnStarted`, la estrategia resuelve todos los símbolos. El `Strategy.Security` principal siempre se añade primero, seguido de cualquier símbolo adicional proporcionado a través de `SymbolsList`.
2. Para cada instrumento resuelto, la estrategia llama a `SubscribeLevel1` y adjunta un callback que recibe actualizaciones `Level1ChangeMessage`.
3. Cada callback verifica que la actualización contenga al menos uno de los campos de precio relevantes (`LastTradePrice`, `BestBidPrice` o `BestAskPrice`).
4. El bid se toma de `BestBidPrice` (o recurre a `LastTradePrice` si el mejor bid falta), el ask viene de `BestAskPrice`, y el spread se calcula si ambos valores están presentes.
5. El logger imprime un mensaje que coincide con el script original: `New tick on the symbol <id> index in the list=<index> bid=<bid> spread=<spread>`. Cuando el ask no está disponible, `spread` se reporta como `n/a`.
6. Si StockSharp no puede encontrar un símbolo solicitado en el `SecurityProvider`, se emite un mensaje de advertencia y el símbolo se omite.

## Instrucciones de uso
1. Asigne el instrumento principal (`Strategy.Security`) a través de la interfaz de configuración de estrategia o en código.
2. Opcionalmente establezca el parámetro `SymbolsList` con identificadores adicionales separados por comas. El orden determina el índice reportado en la salida del log.
3. Conecte la estrategia a una fuente de datos capaz de entregar información Level1 para los instrumentos elegidos.
4. Inicie la estrategia. Se suscribirá inmediatamente a los datos Level1 y comenzará a registrar mensajes de tick.
5. Revise el log de estrategia para verificar los datos de mercado entrantes y los spreads calculados.

## Notas y diferencias frente a la versión MQL
- La versión de StockSharp es completamente impulsada por eventos. No hay bucle manual ni llamada `Sleep`; la plataforma invoca callbacks cuando llegan datos.
- `SymbolsTotal(true)` de MQL se emula preservando el orden en que los instrumentos se añaden a la lista de observación. El índice reportado comienza en cero para el instrumento de estrategia principal.
- Los valores de spread en MetaTrader son enteros basados en puntos. En StockSharp el spread se calcula como una diferencia de precio decimal.
- Los eventos de gráfico personalizados se reemplazan con entradas de log porque las estrategias de StockSharp ya incluyen un subsistema de logging flexible.
- Si un símbolo carece de precio ask en la actualización actual, el spread se reporta como `n/a`, proporcionando claridad sobre información Level1 incompleta.
- La estrategia está diseñada estrictamente para monitoreo y no envía ninguna orden.

## Ejemplo de salida de log
```
New tick on the symbol AAPL@NASDAQ index in the list=0 bid=171.25 spread=0.02
New tick on the symbol MSFT@NASDAQ index in the list=1 bid=324.10 spread=n/a
```
Estas entradas demuestran cómo se reporta la información de bid y spread para cada instrumento rastreado en la lista de Market Watch.
