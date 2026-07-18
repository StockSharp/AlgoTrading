# Estrategia de Trading con RSI Cualificado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el asesor experto de MetaTrader "Trade on qualified RSI" usando la API de alto nivel de StockSharp. Se comporta como un sistema contrario: interpreta lecturas extendidas del Índice de Fuerza Relativa (RSI) como agotamiento y abre una posición contra el movimiento predominante después de que el momentum persiste durante varias velas. Los stops trailing se gestionan en pasos de precio para que el stop siga la operación solo cuando el precio se mueve a favor de la misma.

## Lógica de señal
### Indicador
* Índice de Fuerza Relativa con un período configurable (predeterminado: 28).
* Calculado sobre la suscripción de velas seleccionada (predeterminado: velas de 15 minutos).

### Entrada corta
1. La última vela cerrada tiene RSI mayor o igual al umbral superior (predeterminado: 55).
2. Cada una de las `CountBars` velas cerradas anteriores también tuvo RSI por encima del mismo umbral. Internamente la estrategia cuenta barras consecutivas; la señal se dispara una vez que el contador alcanza `CountBars + 1`.
3. No hay posición activa abierta. Cuando se activa, la estrategia vende a mercado con el volumen configurado y almacena el cierre de la vela como precio de entrada.

### Entrada larga
1. La última vela cerrada tiene RSI menor o igual al umbral inferior (predeterminado: 45).
2. Cada una de las `CountBars` velas cerradas anteriores también tuvo RSI por debajo del mismo umbral (se requieren `CountBars + 1` lecturas consecutivas).
3. No existe posición abierta. Cuando se activa, la estrategia compra a mercado con el volumen configurado y registra el precio de entrada.

## Gestión de posición
* **Stop inicial:** justo después de la entrada el precio de stop se coloca a `StopLossPoints` pasos de precio de la distancia del cierre de entrada (debajo para largos, encima para cortos). Los pasos de precio se obtienen de `Security.PriceStep`; si el instrumento no lo define la estrategia recurre a `1`.
* **Trailing:** en cada vela terminada el stop se ajusta hacia el cierre actual. Para posiciones largas el stop se convierte en `Cierre - StopLossPoints * PriceStep` cuando ese valor está por encima del stop anterior. Para posiciones cortas el stop se convierte en `Cierre + StopLossPoints * PriceStep` cuando ese valor está por debajo del stop anterior.
* **Salida:** si el mínimo de la vela cruza por debajo del stop mientras está largo, o el máximo de la vela cruza por encima del stop mientras está corto, la estrategia sale de toda la posición a mercado. No hay objetivos de ganancia adicionales ni señales de reversión; las nuevas entradas ocurren solo después de que la posición anterior esté cerrada.

## Parámetros
| Nombre | Descripción | Predeterminado |
| ------ | ----------- | -------------- |
| `RsiPeriod` | Longitud de retrospección para el indicador RSI. | 28 |
| `UpperThreshold` | Nivel de RSI que califica una configuración corta. | 55 |
| `LowerThreshold` | Nivel de RSI que califica una configuración larga. | 45 |
| `CountBars` | Cuántas barras anteriores deben mantenerse más allá del umbral (`CountBars + 1` barras consecutivas en total). | 5 |
| `StopLossPoints` | Distancia del stop expresada en pasos de precio. El desplazamiento de precio real es igual a `StopLossPoints * PriceStep`. | 21 |
| `TradeVolume` | Volumen enviado con cada orden de entrada. | 1 |
| `CandleType` | Suscripción de velas usada para los cálculos del indicador. | Velas de 15 minutos |

Todos los parámetros pueden optimizarse. Los umbrales permiten valores decimales, por lo que es posible una sintonía precisa de los límites del RSI.

## Notas de implementación
* La estrategia usa `SubscribeCandles(...).Bind(...)` para alimentar el indicador RSI y reaccionar solo cuando la vela está completamente formada.
* Los valores del RSI no se leen de vuelta desde el indicador por índice; en cambio, los contadores rastrean cuántas velas terminadas consecutivas respetan los umbrales.
* Los stops protectores se simulan dentro de la estrategia. Las órdenes se cierran a mercado cuando se cruza el nivel de stop en lugar de colocar órdenes de stop separadas.
* Se producen mensajes de registro para entradas y salidas, reflejando la salida detallada del asesor experto original.

## Uso
1. Agregue la estrategia a una aplicación StockSharp, asigne el instrumento y portafolio deseados, y configure la serie de velas.
2. Ajuste los umbrales del RSI, el número de barras calificadas y la distancia del stop para que coincidan con la volatilidad del instrumento objetivo.
3. Inicie la estrategia. Monitoree el registro para ver cuándo ocurren las señales y cómo evoluciona el stop trailing.
4. Considere ejecutar el optimizador incorporado para buscar mejores combinaciones de umbrales o distancias de stop para mercados específicos.
