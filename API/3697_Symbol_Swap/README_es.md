# Estrategia de intercambio de símbolos
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de intercambio de símbolos** es el puerto StockSharp de la utilidad MetaTrader 5 "Symbol Swap". El programa MQL5 original abre un panel donde un operador puede ingresar cualquier ticker, cambiar inmediatamente el gráfico actual a ese símbolo y monitorear una ventana de datos compacta con la última hora, precios de OHLC, volumen de ticks y spread. Esta conversión de C# mantiene las mismas responsabilidades y depende exclusivamente de la suscripción de alto nivel de StockSharp API.

## Comportamiento

1. Al inicio, la estrategia resuelve el instrumento a observar. Primero intenta `WatchedSecurityId`; si el campo está vacío, vuelve a `Strategy.Security` que está configurado en el iniciador.
2. Los datos de la vela del `CandleType` elegido se transmiten a través de `SubscribeCandles(...)`. Las barras terminadas brindan el volumen de apertura, máximo, mínimo, cierre y tick que puebla el panel.
3. Los mejores valores de oferta/demanda en tiempo real llegan a través de `SubscribeLevel1(...)`. El diferencial se recalcula en cada actualización de cotización para reflejar la ventana de datos MQL.
4. El bloque formateado se escribe en el registro de estrategia (`OutputMode = Log`) o se representa en un gráfico (`OutputMode = Chart`) con `DrawText(...)`, recreando el panel flotante de MetaTrader.
5. Llamar a `SwapSecurity("TICKER")` durante la ejecución resuelve la nueva seguridad a través de `SecurityProvider.LookupById` y vuelve a suscribir sin problemas tanto la vela como los feeds de Nivel 1 al instrumento solicitado.

La estrategia es sólo informativa; no realiza pedidos. Puede ejecutarse de forma independiente como un panel de mercado o junto con otros robots comerciales.

## Parámetros

| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `CandleType` | Marco de tiempo que define la suscripción de vela utilizada para generar OHLC y datos de volumen de ticks. | `TimeFrame(1 minute)` |
| `WatchedSecurityId` | Identificador de instrumento opcional. Déjelo vacío para usar `Strategy.Security`. | _vacío_ |
| `OutputMode` | Destino de renderizado del bloque de información. Elija entre `Chart` (superposición cerca del precio) o `Log` (registro de estrategia). | `Chart` |

## Métodos públicos

| Método | Descripción |
|--------|-------------|
| `SwapSecurity(string securityId)` | Resuelve el ticker proporcionado a través del `SecurityProvider` activo e inmediatamente cambia el panel a ese símbolo. El método se puede llamar varias veces; cada llamada borra las suscripciones anteriores de vela/Nivel 1 antes de agregar las nuevas fuentes. |

## Notas de uso

- Asegúrese de que el conector exponga el identificador solicitado; de lo contrario, `SecurityProvider.LookupById` genera una excepción.
- Cuando `OutputMode = Chart`, la estrategia crea automáticamente un área de gráfico, dibuja las velas suscritas y superpone el bloque de estado. Para el modo de registro sólo se producen las actualizaciones textuales.
- El volumen de ticks es igual al `TotalVolume` de la vela, que es como MetaTrader informa su recuento de ticks por barra.
- El diferencial se muestra solo cuando están disponibles tanto la mejor oferta como la mejor demanda. De lo contrario, el campo muestra `n/a`.

## Detalles de conversión

- El bucle del temporizador MetaTrader se reemplaza con suscripciones StockSharp. Las velas se activan una vez por barra terminada y las cotizaciones de Nivel 1 actualizan el diferencial en tiempo real.
- Las etiquetas del panel MQL están representadas por un único bloque de texto de varias líneas. El texto utiliza el orden exacto de la herramienta original: tiempo, período, símbolo, cierre, apertura, máximo, mínimo, volumen de tick, extensión.
- Los intercambios de símbolos en tiempo de ejecución ya no necesitan una gestión manual de Market Watch: la estrategia resuelve los instrumentos directamente a través del proveedor de seguridad StockSharp.
- Solo se utilizan llamadas API de alto nivel (`SubscribeCandles`, `SubscribeLevel1`, `DrawText`, `AddInfo`). No hay cálculos manuales de indicadores ni manipulaciones directas del conector, lo que satisface las reglas de codificación del repositorio.
