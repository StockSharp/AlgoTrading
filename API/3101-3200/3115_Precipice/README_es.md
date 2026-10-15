# Estrategia Precipice
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia Precipice es una conversión directa del asesor experto de MetaTrader *Precipice (barabashkakvn's edition)*. El sistema no analiza la estructura del mercado ni usa indicadores; en cambio, espera a que la posición anterior se cierre y luego lanza una moneda para decidir si entrar largo o corto. Si el trader habilita ambas direcciones, cada vela terminada tiene un 50% de probabilidad de generar una nueva posición siempre que la cuenta esté plana. Las órdenes de protección opcionales reflejan el comportamiento de MetaTrader adjuntando la misma distancia de stop-loss y take-profit en "pips" a cada operación.

La implementación en StockSharp mantiene la naturaleza aleatoria del código original y reproduce sus configuraciones de gestión del dinero. Convierte automáticamente la distancia de pip de MetaTrader al paso de precio nativo del instrumento para que el stop-loss y el take-profit permanezcan simétricos independientemente del número de decimales utilizado por el valor.

## Lógica de trading
1. Suscribirse a la serie de velas primaria definida por `CandleType` y procesar solo las velas completadas para que el timing de la operación coincida con la lógica `OnTick` de MetaTrader después de que cierre la barra.
2. Ignorar todas las señales mientras una posición esté abierta. El experto coloca como máximo una operación a la vez.
3. Cuando la estrategia está plana, generar un número aleatorio para la rama de compra. Si `UseBuy` está habilitado y el resultado es inferior a 0.5, enviar una orden de compra de mercado con `TradeVolume` lotes.
4. Si no se abrió ninguna posición larga, generar otro número aleatorio para la rama de venta. Cuando `UseSell` está habilitado y el resultado supera 0.5, enviar una orden de venta de mercado.
5. Tras la entrada, adjuntar órdenes opcionales de stop-loss y take-profit a `StopLossTakeProfitPips` pips de MetaTrader del cierre de la vela. Las órdenes de protección se cancelan automáticamente cuando la posición vuelve a cero.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Marco temporal de 1 minuto | Marco temporal primario procesado por la estrategia. |
| `TradeVolume` | `decimal` | `1` | Tamaño de la orden usado para cada entrada de mercado. |
| `StopLossTakeProfitPips` | `int` | `100` | Distancia (en pips de MetaTrader) entre el precio de entrada y ambas órdenes de protección. Establezca en `0` para deshabilitar la colocación de stop-loss y take-profit. |
| `UseBuy` | `bool` | `true` | Habilitar entradas largas aleatorias. |
| `UseSell` | `bool` | `true` | Habilitar entradas cortas aleatorias. |

## Diferencias con el experto original de MetaTrader
- MetaTrader expone los niveles de freeze y stop del instrumento; el puerto de StockSharp emula solo la conversión de distancia en pips y depende del broker para rechazar distancias de stop inválidas si es necesario.
- El EA original usa las cotizaciones actuales de Bid/Ask. La estrategia de StockSharp basa las órdenes de protección en el precio de cierre de la vela porque la API de alto nivel recibe datos de vela agregados; los efectos de slippage y spread deben manejarse externamente.
- MetaTrader trabaja con tickets individuales, mientras que StockSharp gestiona posiciones netas. La conversión mantiene como máximo una posición neta y elimina las órdenes de protección en cuanto la exposición vuelve a cero.

## Consejos de uso
- Elija un `TradeVolume` realista que coincida con el paso de lote del valor. El constructor también aplica este valor a `Strategy.Volume` para que los métodos auxiliares envíen órdenes con la cantidad deseada.
- Ajuste `StopLossTakeProfitPips` según la volatilidad del instrumento. La estrategia multiplica los pips por el paso de precio del valor (escalado para cotizaciones de 3/5 dígitos) para obtener una distancia de precio nativa.
- Habilite solo `UseBuy` o `UseSell` si desea que el generador aleatorio abra operaciones en una sola dirección, por ejemplo para probar controles de riesgo direccional.
- Dado que las entradas son aleatorias, monitoree la estrategia con límites de riesgo adicionales o una duración máxima de posición si necesita condiciones de salida deterministas.

## Indicadores
- Ninguno. La estrategia se basa puramente en la generación aleatoria de operaciones y órdenes de protección opcionales.
