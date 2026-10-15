# Estrategia de ruptura del indicador Vortex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia transfiere el experto MetaTrader **Vortex Indicator System.mq4** al StockSharp nivel alto API. La idea original era
Se publica en *Análisis técnico de acciones y materias primas* (enero de 2010) y se basa en el cruce del indicador Vortex para armar bre.
Órdenes akout en el máximo/mínimo de la vela cruzada. La versión StockSharp mantiene el mismo flujo de decisión: un cruce cierra el
posición opuesta, activa un disparador de ruptura en el extremo de la barra de cruce, y la siguiente vela que rompe ese nivel ejecuta el
orden del mercado.

## como funciona

1. Se abre una suscripción de vela única según `CandleType`. La transmisión resultante está vinculada a una instancia `VortexIndicator`
Una vez que se usa `Bind`, la estrategia siempre recibe valores VI+ y VI- sincronizados para las velas terminadas.
2. Cuando el indicador termina de calentarse, el algoritmo rastrea los valores VI anteriores para detectar las mismas condiciones de cruce u
sed en el experto MQL: `VI+` cruzando por encima de `VI-` o viceversa entre las dos últimas velas cerradas.
3. **Fase de configuración**: tan pronto como se detecta un cruce alcista, cualquier posición corta abierta se cierra inmediatamente y el máximo del
La vela cruzada se convierte en el disparador largo pendiente. El cruce opuesto cierra una posición larga existente y almacena el mínimo.
de esa barra como disparador corto.
4. **Fase de activación**: en cada vela finalizada posterior, la estrategia verifica si se tocó el precio de activación registrado ("Hola
ghPrice` ≥ long trigger or `LowPrice` ≤ disparador corto). Si es así, envía una orden de mercado de tamaño suficiente para aplanar al oponente restante.
exposición del sitio (si el pedido anterior aún no se completó) y abra una nueva posición con `TradeVolume`.
5. Una vez que se activa una orden, se borra el activador correspondiente. Si no se produce ninguna ruptura, la configuración permanece activa hasta un nuevo cruce.
r lo anula.
6. Las salidas se basan exclusivamente en la lógica de cruce: la señal opuesta inmediatamente aplana la posición actual y arma una nueva b.
desencadenador de consulta, que refleja la implementación de MetaTrader.

## Señales

- **Configuración alcista**: ocurre cuando `VI+` estaba por debajo o igual a `VI-` en la vela cerrada anterior y se eleva por encima de ella en la vela más alta.
reciente. El disparador largo se fija en el máximo de esa vela.
- **Ejecución alcista**: la siguiente vela cuyo máximo alcanza el disparador envía una orden de compra de mercado usando `TradeVolume` (más cualquier vo
volumen necesario para cerrar una posición corta pendiente).
- **Configuración bajista**: ocurre cuando `VI-` estaba por debajo o igual a `VI+` en la vela cerrada anterior y se eleva por encima de ella en la vela más alta.
reciente. El disparador corto se fija en el mínimo de esa vela.
- **Ejecución bajista**: la siguiente vela cuyo mínimo toca el disparador envía una orden de venta de mercado usando `TradeVolume` (más el vo
lume necesario para aplanar una posición larga abierta).

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|---------|-------------|
| `VortexLength` | 14 | Período aplicado al indicador Vortex. |
| `CandleType` | 1 hora | Marco de tiempo utilizado para velas y actualizaciones de indicadores. |
| `TradeVolume` | 1 | Tamaño de orden de mercado utilizado para nuevas entradas. |

## Notas de implementación

- La estrategia solo reacciona a velas **terminadas** para cumplir con las pautas de conversión. Las rupturas intrabar se reconocen como
tan pronto como una vela se cierra con un máximo/mínimo más allá del disparador almacenado.
- Los activadores pendientes se borran el `OnStopped` para que la instancia se pueda reiniciar limpiamente sin que quede ningún estado.
- Al ejecutar una orden de ruptura, el algoritmo aumenta el volumen si aún mantiene una posición opuesta, logrando la misma e
Funciona como el experto MetaTrader, que cerró la orden activa antes de abrir la nueva.
