# Estrategia de Gestión de Órdenes Trading Boxing
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La Estrategia de Gestión de Órdenes Trading Boxing recrea el panel de gestión manual de órdenes del asesor experto TradingBoxing original. En lugar de botones en el gráfico, la versión de StockSharp expone parámetros que pueden activarse desde la interfaz de estrategia o la automatización. Cada interruptor ejecuta inmediatamente la acción solicitada y luego se restablece, proporcionando una superficie de control conveniente para entradas al mercado, colocación de órdenes pendientes y limpieza de posiciones existentes.

La estrategia no depende de lógica de indicadores ni de eventos de datos de mercado. Simplemente coordina el envío y cancelación de órdenes para el instrumento y la cartera asignados a la instancia de estrategia.

## Parámetros
### Configuración de volumen
- `BuyVolume` – cantidad usada cuando se activa la acción *Open Buy Market*. Debe ser positiva.
- `SellVolume` – cantidad usada cuando se activa la acción *Open Sell Market*. Debe ser positiva.
- `BuyStopVolume` – cantidad para nuevas órdenes de stop de compra.
- `BuyLimitVolume` – cantidad para nuevas órdenes de límite de compra.
- `SellStopVolume` – cantidad para nuevas órdenes de stop de venta.
- `SellLimitVolume` – cantidad para nuevas órdenes de límite de venta.

### Configuración de precio
- `BuyStopPrice` – precio de activación para órdenes de stop de compra.
- `BuyLimitPrice` – precio para órdenes de límite de compra.
- `SellStopPrice` – precio de activación para órdenes de stop de venta.
- `SellLimitPrice` – precio para órdenes de límite de venta.

### Interruptores de acción
Todos los parámetros de acción son interruptores booleanos. Establecer un interruptor en `true` realiza la tarea correspondiente y la estrategia lo vuelve a establecer en `false` en el mismo ciclo de procesamiento.

- `CloseBuyPositions` – cierra la exposición larga actual (si `Position > 0`).
- `CloseSellPositions` – cierra la exposición corta actual (si `Position < 0`).
- `DeleteBuyStops` – cancela las órdenes de stop de compra rastreadas.
- `DeleteBuyLimits` – cancela las órdenes de límite de compra rastreadas.
- `DeleteSellStops` – cancela las órdenes de stop de venta rastreadas.
- `DeleteSellLimits` – cancela las órdenes de límite de venta rastreadas.
- `OpenBuyMarket` – envía una orden de compra a mercado usando `BuyVolume`.
- `OpenSellMarket` – envía una orden de venta a mercado usando `SellVolume`.
- `PlaceBuyStop` – registra una nueva orden de stop de compra en `BuyStopPrice` con `BuyStopVolume` y la almacena para cancelación posterior.
- `PlaceBuyLimit` – registra una nueva orden de límite de compra en `BuyLimitPrice` con `BuyLimitVolume` y la almacena para cancelación posterior.
- `PlaceSellStop` – registra una nueva orden de stop de venta en `SellStopPrice` con `SellStopVolume` y la almacena para cancelación posterior.
- `PlaceSellLimit` – registra una nueva orden de límite de venta en `SellLimitPrice` con `SellLimitVolume` y la almacena para cancelación posterior.

## Detalles de comportamiento
- Las órdenes creadas a través de las acciones de órdenes pendientes se rastrean internamente para que las acciones de eliminación puedan cancelarlas posteriormente. Las órdenes externas que no fueron colocadas por esta estrategia no se ven afectadas.
- La estrategia verifica que está en ejecución y que tanto `Security` como `Portfolio` están asignados antes de ejecutar cualquier solicitud. Cuando falta un requisito, registra una advertencia e ignora el interruptor.
- La validación de volumen y precio replica las salvaguardias del panel original: cualquier cantidad no positiva activa una advertencia y no se envía ninguna orden.
- Las acciones de cierre operan sobre la posición neta mantenida por la estrategia. Si se necesita cubrir un corto, la estrategia envía una orden de compra a mercado igual a `Math.Abs(Position)`; para una posición larga, envía una orden de venta a mercado del valor actual de `Position`.

## Notas de uso
1. Inicie la estrategia con una cartera y un instrumento válidos.
2. Ajuste los parámetros de volumen y precio para que coincidan con el instrumento que opera.
3. Active acciones manuales estableciendo el parámetro booleano requerido en `true`. La propiedad revierte automáticamente a `false` después de que la acción se completa, por lo que el siguiente activador está listo de inmediato.
4. Use los interruptores de eliminación para borrar órdenes pendientes colocadas previamente siempre que cambie el plan de operación.

Dado que la estrategia está puramente dirigida por eventos de entrada del usuario, no hay necesidad de suscribirse a velas o cotizaciones. Actúa como un asistente de ejecución simple, reflejando la flexibilidad de la interfaz TradingBoxing original dentro del entorno de StockSharp.
