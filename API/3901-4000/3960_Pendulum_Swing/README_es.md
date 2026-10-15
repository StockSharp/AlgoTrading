# Estrategia de oscilación del péndulo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Pendulum Swing** es una versión StockSharp del asesor experto MetaTrader *Pendulum 1_01*. El sistema original mantiene dos órdenes stop pendientes alrededor del precio actual y aumenta progresivamente su volumen después de cada ejecución. Esta versión de C# reproduce el mismo comportamiento de "swing" utilizando ayudantes StockSharp de alto nivel.

Ideas clave:

- Mantenga órdenes simétricas de parada de compra y venta a una distancia configurable desde la última vela cerrada.
- Después de cada llenado, la siguiente parada del mismo lado multiplica su volumen, implementando la progresión estilo martingala desde la fuente MQL.
- Cierre la posición cuando se alcance un objetivo de pip a corto plazo o cuando el capital de la cuenta cruce los umbrales globales de ganancias/pérdidas.

## como funciona
1. Cuando comienza la estrategia, se suscribe a una serie de velas definida por el usuario (predeterminado: 15 minutos) y, opcionalmente, a velas diarias. El rango diario controla la distancia entre el precio de mercado y las paradas pendientes.
2. En cada vela comercial terminada, el algoritmo:
   - Actualiza los límites globales basados en acciones.
   - Verifica si la posición actual alcanzó el objetivo de ganancias local.
   - Calcula la distancia de parada a partir del último rango diario o de la entrada manual de pips, y luego coloca/actualiza las órdenes buy-stop y sell-stop.
3. Cuando se ejecuta una orden stop, el nivel de progresión correspondiente avanza, por lo que la siguiente parada en ese lado utiliza el volumen multiplicado. Una vez que se alcanza `MaxLevels`, no se crean nuevas órdenes para esa dirección hasta que la posición vuelva a cero.
4. Después de cada vela se ejecutan comprobaciones globales de toma de ganancias/detención de pérdidas y liquidan la cartera si se superan los umbrales de capital configurados.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| ---- | ---- | ------- | ----------- |
| `BaseVolume` | `decimal` | `0.1` | Volumen de la primera parada pendiente. |
| `VolumeMultiplier` | `decimal` | `2` | Factor aplicado después de cada nivel llenado en el mismo lado. |
| `MaxLevels` | `int` | `8` | Número máximo de rellenos permitidos en una dirección. |
| `ManualStepPips` | `int` | `50` | Distancia de parada en pips cuando el rango diario no está disponible. |
| `UseDynamicRange` | `bool` | `true` | Si está habilitado, deriva el paso de la última vela diaria terminada. |
| `RangeFraction` | `decimal` | `0.2` | Fracción del rango diario utilizada como distancia de parada base. |
| `TakeProfitPips` | `int` | `10` | Objetivo de pip local que cierra la posición actual. Establezca `0` para desactivar. |
| `SlippagePips` | `int` | `3` | Se agregó un buffer adicional a la distancia pendiente para imitar el deslizamiento MetaTrader. |
| `UseGlobalTargets` | `bool` | `true` | Permite controles de liquidación basados en acciones. |
| `GlobalTakePercent` | `decimal` | `1` | Crecimiento de las acciones (en porcentaje) que desencadena la toma de ganancias global. |
| `GlobalStopPercent` | `decimal` | `2` | Reducción de capital (en porcentaje) que desencadena un stop-loss global. |
| `CandleType` | `DataType` | `15m` velas | Marco de tiempo utilizado para la lógica comercial principal. |

## Notas
- El tamaño de la posición respeta el paso de volumen del instrumento y los ajustes de volumen mínimo y máximo.
- Los precios de parada se ajustan al escalón del precio del instrumento y evitan el reemplazo constante de órdenes respetando una tolerancia de precio.
- Los objetivos globales dependen de `Portfolio.CurrentValue` (o `BeginValue` como alternativa), por lo que la cartera seleccionada debe exponer esta información.
- La estrategia utiliza `StartProtection()` para activar la protección de posición incorporada de StockSharp una vez al inicio.

## Diferencias de conversión
- Se omiten los dibujos de etiquetas de la interfaz de usuario y las tablas de saldos de cuentas del script MQL original.
- Los niveles globales de toma de ganancias siguen umbrales de capital basados en porcentajes en lugar de la aritmética de valor de tick bruto utilizada en MQL, lo que mantiene el comportamiento consistente entre los corredores.
- Las funciones específicas de MetaTrader, como `OrderModify`, se reemplazan con StockSharp rutinas de cancelación y reenvío de pedidos.
