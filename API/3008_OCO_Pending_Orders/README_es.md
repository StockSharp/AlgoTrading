# Estrategia de Órdenes Pendientes OCO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Órdenes Pendientes OCO** replica el comportamiento del asesor experto MetaTrader4 `OCO_EA.mq4` dentro de la API de alto nivel de StockSharp. El algoritmo permite a un trader armar hasta cuatro disparadores de precio independientes (buy limit, buy stop, sell limit, sell stop). Cuando el mejor bid o ask en vivo toca el nivel de precio configurado, la estrategia envía una orden de mercado inmediata, cancelando opcionalmente todos los demás disparadores pendientes de forma clásica "uno cancela los otros" (OCO).

La estrategia depende puramente de datos de mercado de nivel 1 – no se requieren indicadores históricos. Está pensada para flujos de trabajo de trading discrecional o semi-automatizado donde los traders definen manualmente los niveles de precio y quieren que la plataforma ejecute tan pronto como se toque el nivel, mientras también adjunta órdenes de salida protectoras.

## Lógica de negociación
1. El trader establece cualquier combinación de los cuatro precios disparadores y activa el parámetro **Armed** a `true`.
2. La estrategia se suscribe a actualizaciones de nivel 1 y mantiene el último mejor bid y ask en memoria.
3. En cada actualización compara los precios almacenados con los umbrales configurados:
   - Si el mejor ask es *menor o igual que* el precio **Buy limit**, se envía una orden de compra de mercado con el volumen configurado.
   - Si el mejor ask es *mayor o igual que* el precio **Buy stop**, se envía una orden de compra de mercado.
   - Si el mejor bid es *mayor o igual que* el precio **Sell limit**, se envía una orden de venta de mercado.
   - Si el mejor bid es *menor o igual que* el precio **Sell stop**, se envía una orden de venta de mercado.
4. Después de cada disparador ejecutado, el nivel correspondiente se borra (se devuelve a cero). Cuando **Use OCO link** está habilitado, todos los demás niveles se borran inmediatamente, reflejando el comportamiento original de MT4. Cuando el enlace OCO está deshabilitado, los demás niveles permanecen activos hasta que disparan o se borran manualmente.
5. Si todos los precios disparadores son cero, la estrategia se desarma automáticamente cambiando **Armed** de vuelta a `false`.

Todas las ejecuciones se realizan con llamadas `BuyMarket` y `SellMarket` para garantizar rellenos inmediatos que respeten el enrutamiento de intercambio configurado en el entorno StockSharp. Se producen entradas de registro informativas para cada disparador para simplificar el monitoreo.

## Parámetros
- **Order volume** – volumen enviado con cada orden de mercado. El valor debe ser positivo.
- **Buy limit price** – umbral de precio ask que activa una entrada larga de estilo límite. Establecer en `0` para deshabilitar.
- **Buy stop price** – umbral de precio ask que activa una entrada larga de estilo stop. Establecer en `0` para deshabilitar.
- **Sell limit price** – umbral de precio bid que activa una entrada corta de estilo límite. Establecer en `0` para deshabilitar.
- **Sell stop price** – umbral de precio bid que activa una entrada corta de estilo stop. Establecer en `0` para deshabilitar.
- **Stop loss (pips)** – distancia en puntos del instrumento para el stop de protección. Convertido a precio multiplicando por `Security.PriceStep` (respaldo `1` cuando el instrumento no reporta un tamaño de tick).
- **Take profit (pips)** – distancia en puntos del instrumento para el objetivo de beneficio. Se usa la misma lógica de conversión que para el stop loss.
- **Use OCO link** – si es `true`, la primera orden rellena borra los niveles de precio restantes y desarma la estrategia. Si es `false`, los niveles restantes permanecen activos hasta que se disparen individualmente.
- **Armed** – interruptor de seguridad que habilita o deshabilita la lógica de trading. La estrategia lo restablece automáticamente a `false` cuando no quedan niveles de disparadores activos.

## Gestión de riesgos
`StartProtection` está habilitado durante `OnStarted`, adjuntando offsets de stop-loss y take-profit de precio absoluto a cada posición abierta. Los offsets se derivan de los parámetros **Stop loss (pips)** y **Take profit (pips)** usando el tamaño del tick del instrumento. Las órdenes de protección siempre se envían como órdenes de mercado para garantizar la ejecución de salida incluso cuando el instrumento subyacente es ilíquido.

Como la estrategia es puramente basada en eventos, no mantiene órdenes límite pendientes en el intercambio; reacciona a los datos del mercado y envía órdenes de mercado, igual que la versión MQL original que emitía órdenes inmediatas y luego las modificaba para aplicar las distancias de stop-loss y take-profit.

## Consejos de uso
1. Configurar el instrumento, portafolio y conexión dentro de StockSharp como de costumbre.
2. Establecer **Order volume** para que coincida con el tamaño de lote deseado.
3. Ingresar cualquier subconjunto de precios disparadores y cambiar **Armed** a `true`. Los valores dejados en `0` se ignoran.
4. Opcionalmente deshabilitar **Use OCO link** para mantener los disparadores restantes activos después del primer relleno.
5. Monitorear el registro para mensajes que confirmen cada disparador y el estado de restablecimiento automático.

Recuerde que la estrategia usa el paso de precio proporcionado por el broker. Si el instrumento de trading cotiza en pips fraccionarios o usa tamaños de tick no convencionales, ajuste las distancias basadas en pips en consecuencia antes de armar la estrategia.

## Diferencias respecto al script MQL original
- La estrategia usa el ayudante `StartProtection` de StockSharp en lugar de modificar manualmente las órdenes para aplicar niveles de stop-loss y take-profit.
- Las suscripciones de datos de nivel 1 se manejan a través de enlaces de alto nivel en lugar de encuestas manuales de los valores `Bid`, `Ask`, `High` y `Low`.
- Los parámetros se exponen a través de `StrategyParam<T>` para que puedan ajustarse y optimizarse directamente en la UI de StockSharp.
- El registro reemplaza las notificaciones `Comment` y `PlaySound` de MT4, proporcionando transparencia de ejecución dentro de los registros de StockSharp.
