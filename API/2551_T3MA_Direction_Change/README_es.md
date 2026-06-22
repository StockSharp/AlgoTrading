# Estrategia T3 MA de Cambio de Dirección
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el comportamiento del asesor experto original **T3MA(barabashkakvn's edition)**. El Asesor Experto depende del indicador "T3MA-ALARM" que aplica suavizado exponencial dos veces y genera una señal cuando la línea suavizada cambia de dirección. El port en StockSharp mantiene el mismo concepto: crea una media móvil exponencial de doble suavizado (EMA de EMA) y opera cada vez que la pendiente de esa curva cambia de bajante a creciente o viceversa.

La estrategia opera solo en velas finalizadas. Las señales pueden retrasarse un número configurable de barras para imitar la opción original `InpBarNumber` (el retardo predeterminado es una barra). Las órdenes se colocan usando ejecución de mercado para que la cartera cambie entre exposición larga y corta sin acumular múltiples posiciones hedgeadas concurrentes.

## Reglas de trading
1. Suscribirse a la serie de velas configurada y calcular una EMA de los precios de cierre. Ejecutar una segunda EMA sobre el resultado de la primera EMA, produciendo la serie suavizada utilizada por el indicador.
2. Comparar el valor actual de la serie suavizada (opcionalmente desplazado hacia adelante por `EMA Shift`) con el valor anterior. La pendiente se considera alcista cuando la serie aumenta y bajista cuando disminuye.
3. Cuando la pendiente cambia de bajista a alcista, encolar una señal de **compra**. Cuando cambia de alcista a bajista, encolar una señal de **venta**. Las velas neutras insertan una señal cero en la cola para que el contador de retardo permanezca preciso.
4. Después de que pase el número configurado de velas completadas `Signal Delay`, ejecutar la señal encolada. Una compra retrasada cierra cualquier posición corta abierta y entra largo con el `Trade Volume` base. Igualmente, una venta retrasada cierra una posición larga y entra corto.
5. Las órdenes de stop-loss y take-profit protectoras se inicializan via `StartProtection`. Ambas distancias se expresan en pasos de precio para adaptarse automáticamente al tamaño del tick del instrumento seleccionado.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `EMA Length` | Longitud de la EMA utilizada para ambos pasos de suavizado. Corresponde al parámetro `MAPeriod` en la implementación de MetaTrader. |
| `EMA Shift` | Número de barras por las que la EMA suavizada se desplaza antes de comparar pendientes. Equivalente al `MAShift` del indicador. |
| `Signal Delay` | Número de velas completadas a esperar antes de ejecutar una señal. Refleja `InpBarNumber`, por lo que un valor de 1 opera la señal de la barra anterior. |
| `Stop Loss (steps)` | Distancia del stop-loss medida en pasos de precio. Establecer en cero para desactivar la protección. |
| `Take Profit (steps)` | Distancia del take-profit medida en pasos de precio. Establecer en cero para desactivar. |
| `Trade Volume` | Tamaño de orden base usado para nuevas entradas. Al revertir una posición, la estrategia agrega el tamaño absoluto de la posición actual a este valor. |
| `Candle Type` | Tipo de datos de vela usado para cálculos (por defecto: marco temporal de 5 minutos). |

## Gestión de riesgo
* `StartProtection` registra automáticamente niveles de stop-loss y take-profit cuando comienza la estrategia. Ambos niveles siguen el tamaño del tick del instrumento y permanecen activos durante toda la vida de la estrategia.
* Los giros de posición se ejecutan usando órdenes de mercado. Cuando la dirección de la señal coincide con la exposición actual, no se emiten operaciones adicionales, evitando pirámides no deseadas.
* Los registros se emiten en cada operación para rastrear la razón y el precio de referencia tomado de la vela fuente.

## Diferencias con la versión MQL5
* MetaTrader 5 requería una cuenta de hedging y podía acumular múltiples posiciones. La versión StockSharp mantiene una sola posición neta y la revierte cuando se activa la señal opuesta.
* El procesamiento de señales está basado en velas y ocurre una vez por vela finalizada en lugar de en cada tick, lo cual es más natural dentro de la API de alto nivel de StockSharp.
* La gestión de stop-loss y take-profit se maneja via `StartProtection` en lugar de enviar manualmente precios SL/TP con cada orden.
* Se añadieron comentarios en inglés, parámetros estructurados y asistentes de gráficos para mejor legibilidad en el entorno StockSharp.

## Notas de uso
1. Adjuntar la estrategia al instrumento deseado y asegurarse de que el tipo de vela coincide con el marco temporal que se usó al optimizar el Asesor Experto original.
2. Ajustar `EMA Length` y los parámetros de riesgo para adaptarse a la volatilidad del instrumento. Los retrasos mayores (`Signal Delay`) ralentizan las respuestas y pueden filtrar ruido.
3. Dado que la estrategia trabaja con pasos de precio, verificar que la propiedad `PriceStep` del instrumento esté configurada correctamente para que las órdenes protectoras se coloquen a distancias significativas.
