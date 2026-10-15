# Estrategia de bomba de tiempo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Time Bomb replica el asesor experto MetaTrader que dispara una sola orden cada vez que el precio explota en una dirección dentro de un
ventana corta y configurable. La estrategia observa las mejores cotizaciones de oferta/demanda en tiempo real y mide el número de pips cubiertos entre
el último precio de referencia y la cotización más reciente. Si la distancia requerida se recorre lo suficientemente rápido, se abre una orden de mercado en
la dirección de la ruptura e inmediatamente arma niveles ocultos de stop-loss y take-profit expresados en pips.

La implementación actúa solo cuando no hay ninguna posición abierta actualmente, reflejando la lógica del bloque original que evitó la superposición.
oficios. Las referencias de precios se restablecen una vez que se activa una señal o cuando expira la ventana de observación, por lo que cada ráfaga de
la volatilidad produce como máximo una única operación por lado. Los niveles de stop-loss y take-profit se mantienen internamente y se hacen cumplir por
la estrategia en sí porque StockSharp no coloca automáticamente órdenes de protección para ejecuciones de mercado.

## Detalles

- **Criterios de entrada**:
  - **Largo**: La mejor demanda aumenta al menos `BuyPipsInTime` pips en comparación con el precio de referencia almacenado y el movimiento finaliza
en `BuyTimeToWait` segundos. Una orden de compra con tamaño `BuyVolume` se envía una vez que se cumple la condición.
  - **Corto**: La mejor oferta cae al menos `SellPipsInTime` pips en comparación con el precio de referencia almacenado y el movimiento finaliza
en `SellTimeToWait` segundos. Una orden de venta con tamaño `SellVolume` se envía una vez que se cumple la condición.
- **Largo/Corto**: Se admiten ambas direcciones, pero solo puede existir una posición a la vez.
- **Criterios de salida**:
  - **Largo**: la posición se cierra cuando la mejor oferta toca el precio calculado de stop-loss o take-profit.
  - **Corto**: La posición se cierra cuando la mejor oferta alcanza el límite de pérdidas calculado o la mejor oferta alcanza el nivel de obtención de ganancias.
- **Paradas**: La estrategia maneja las paradas de protección ocultas. Las distancias se definen en pips y se traducen a precios utilizando
el tamaño de paso del símbolo actual.
- **Valores predeterminados**:
  - `SellPipsInTime` = 5 pips, `SellTimeToWait` = 10 segundos, `SellVolume` = 0,01 lotes.
  - `SellStopLossPips` = 20 pips, `SellTakeProfitPips` = 20 pips.
  - `BuyPipsInTime` = 5 pips, `BuyTimeToWait` = 10 segundos, `BuyVolume` = 0,01 lotes.
  - `BuyStopLossPips` = 20 pips, `BuyTakeProfitPips` = 20 pips.
- **Filtros**:
  - Categoría: Ruptura/impulso.
  - Dirección: Simétrica (larga y corta).
  - Indicadores: Solo movimiento de precios brutos, sin osciladores.
  - Paradas: Sí (distancias de puntos fijas por lado).
  - Complejidad: Baja: detector de ruptura único con seguimiento de estado simple.
  - Marco temporal: intradiario, reacciona a los impulsos a nivel de tick una vez por segundo.
  - Estacionalidad: No.
  - Redes neuronales: No.
  - Divergencia: No.
  - Nivel de riesgo: Depende de las distancias de pips configuradas; los incumplimientos corresponden a un riesgo medio en los principales pares de divisas.

## Entradas

| Nombre | Descripción |
| --- | --- |
| `SellPipsInTime` | Distancia mínima descendente en pips que se debe recorrer antes de abrir una posición corta. |
| `SellTimeToWait` | Unos segundos permitieron que se completara el movimiento descendente. |
| `SellVolume` | Volumen comercial para señales de venta. |
| `SellStopLossPips` | Distancia de stop-loss para posiciones cortas, expresada en pips. |
| `SellTakeProfitPips` | Distancia de toma de ganancias para posiciones cortas, expresada en pips. |
| `BuyPipsInTime` | Distancia mínima hacia arriba en pips que se debe cubrir antes de abrir una posición larga. |
| `BuyTimeToWait` | Unos segundos permitieron que se completara el movimiento ascendente. |
| `BuyVolume` | Volumen comercial para señales de compra. |
| `BuyStopLossPips` | Distancia de stop-loss para posiciones largas, expresada en pips. |
| `BuyTakeProfitPips` | Distancia de obtención de beneficios para posiciones largas, expresada en pips. |

## Notas

- La estrategia se basa en las mejores actualizaciones de oferta/demanda; asegúrese de que la fuente de datos proporcione cotizaciones precisas de nivel uno.
- Establecer cualquier distancia de pip o ventana de tiempo en cero desactiva la señal correspondiente porque el precio de referencia se reinicia en lugar de
generando operaciones.
- Debido a que los niveles de protección se gestionan internamente, las desconexiones inesperadas pueden dejar posiciones sin paradas bruscas. considerar
combinando la estrategia con controles de riesgo externos cuando se ejecuta en vivo.
