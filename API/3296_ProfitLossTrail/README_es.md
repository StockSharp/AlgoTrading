# Estrategia ProfitLossTrailStrategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

ProfitLossTrailStrategy es un ayudante de gestión de riesgos convertido desde el asesor experto de MetaTrader **ProfitLossTrailEA v2.30**. La estrategia no genera entradas por sí misma. En su lugar, supervisa la posición actualmente abierta en el valor configurado y aplica automáticamente salidas de protección:

- niveles iniciales de stop-loss y take-profit;
- gestión de trailing stop con distancia de activación opcional y control de paso trailing;
- protección break-even con disparador de ganancia y desplazamiento configurables;
- capacidad de eliminar niveles de protección existentes cuando el trader quiere gestionarlos manualmente.

El comportamiento coincide estrechamente con el modo de gestión de "cesta" del EA original: todas las órdenes de la misma dirección se tratan como una sola posición y los niveles de protección se recalculan cuando cambia la exposición.

## Referencia de parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Manage As Basket** | Cuando está habilitado (predeterminado), cada ejecución en la misma dirección recalcula el precio medio de entrada y refresca niveles de stop-loss/take-profit. Desactive el flag para conservar los niveles iniciales después de la primera ejecución. |
| **Enable Take Profit** | Activa o desactiva la gestión automática de take-profit. |
| **Take Profit (pips)** | Distancia en pips entre el precio de entrada y el objetivo de take-profit. |
| **Enable Stop Loss** | Activa o desactiva la gestión automática de stop-loss. |
| **Stop Loss (pips)** | Distancia en pips entre el precio de entrada y el stop de protección inicial. |
| **Enable Trailing Stop** | Activa la gestión dinámica del stop cuando la posición está en ganancia. |
| **Trailing Activation (pips)** | Ganancia mínima en pips requerida antes de que el trailing stop pueda moverse. Use `0` para activar inmediatamente. |
| **Trailing Stop (pips)** | Distancia trailing base expresada en pips. |
| **Trailing Step (pips)** | Ganancia adicional que debe obtenerse antes de ajustar más el trailing stop. |
| **Enable Break-Even** | Habilita la rutina break-even que mueve el stop a ganancia después de una distancia disparadora. |
| **Break-Even Trigger (pips)** | Distancia de ganancia que activa el movimiento a break-even. |
| **Break-Even Offset (pips)** | Desplazamiento extra añadido por encima (largo) o por debajo (corto) del precio de entrada cuando break-even se activa. |
| **Remove Take Profit** | Cuando se establece en `true`, se limpia cualquier valor actual de take-profit y no se emiten salidas take-profit. |
| **Remove Stop Loss** | Cuando se establece en `true`, se limpia cualquier valor actual de stop-loss y no se emiten salidas stop-loss ni trailing. |
| **Candle Type** | Serie de velas usada para monitorizar la acción del precio. Las comprobaciones de trailing, break-even y salida se evalúan en velas finalizadas. |

## Notas de uso

1. Adjunte la estrategia a un valor y asegúrese de que las órdenes se coloquen externamente o por otra estrategia. ProfitLossTrailStrategy se centra exclusivamente en gestionar la exposición abierta.
2. Configure los parámetros basados en pips para que coincidan con la cotización del instrumento. El tamaño de pip se deriva automáticamente de `Security.PriceStep`.
3. Cuando tanto break-even como trailing stop están habilitados, el ajuste de break-even ocurre primero. Los pasos trailing posteriores solo ajustarán el stop si el nuevo nivel mejora el precio de protección actual al menos por la distancia de paso trailing especificada.
4. Establecer **Remove Stop Loss** desactiva simultáneamente stop-loss, trailing y lógica break-even, reflejando el comportamiento del EA original.
5. La estrategia usa órdenes de mercado (`BuyMarket`/`SellMarket`) para cerrar posiciones cuando se alcanzan niveles de protección.

## Notas de conversión

- Los modos "Order_By_Order" y "Same_Type_As_One" de MetaTrader se representan mediante el flag **Manage As Basket**. Gestionar niveles de stop por ticket no está soportado en StockSharp, por lo que el modo cesta se aplica por defecto.
- Los filtros de magic number y comentario del EA original no son necesarios; la estrategia actúa solo sobre `Strategy.Security` configurado.
- El dibujo en pantalla, las alertas sonoras y las actualizaciones de UI basadas en temporizador se omitieron porque StockSharp ya expone diagnósticos mediante logs y vinculaciones de gráfico.
