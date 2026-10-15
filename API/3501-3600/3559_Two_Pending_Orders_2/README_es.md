# Dos pedidos pendientes 2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un puerto StockSharp del asesor experto MetaTrader **"Dos órdenes pendientes 2"**. Mantiene dos órdenes pendientes simétricas alrededor del precio de mercado y permite que el primer lado activado gestione la operación con reglas configurables de stop-loss, take-profit y trailing. La conversión utiliza el nivel alto StockSharp API y mantiene las ideas centrales del algoritmo original al tiempo que expone cada perilla de ajuste a través de los parámetros de la estrategia.

## Lógica comercial
1. La estrategia se suscribe a la serie de velas seleccionada (velas diarias por defecto). Cuando se termina una vela, se convierte en el punto de decisión para el siguiente ciclo comercial.
2. Las órdenes pendientes activas se cancelan una vez que caducan o antes de que se realicen nuevas órdenes. Esto garantiza que solo haya los niveles más frescos del mercado.
3. Si el diferencial actual está dentro del umbral permitido y el recuento de posiciones/órdenes activas está por debajo del límite configurado, la estrategia coloca dos órdenes pendientes simétricas:
   - **Modo Stop** (predeterminado) coloca un stop de compra por encima del mercado y un stop de venta por debajo de él.
   - **Modo límite** coloca un límite de compra por debajo del mercado y un límite de venta por encima de él.
   - El indicador *Niveles inversos* intercambia los tipos de órdenes para replicar el cambio inverso EA original.
4. Los precios de entrada se compensan con la oferta/demanda actual mediante el parámetro *Pending Sangría*. Las órdenes se omiten cuando están más cerca que la distancia *Paso mínimo* de las posiciones existentes.
5. Las órdenes pendientes pueden caducar después de un número determinado de minutos. Cuando se alcanza el vencimiento, todos los pedidos restantes se cancelan.

## Gestión de posiciones
- Una vez que se completa una orden, la estrategia rastrea el precio de entrada promedio y el volumen del lado correspondiente. Los rellenos opuestos reducen o cierran la posición existente antes de abrir una nueva.
- La estrategia sale de posiciones largas cuando el precio alcanza cualquiera de estas condiciones:
  - El precio toca la distancia de stop-loss por debajo del precio de entrada promedio.
  - El precio alcanza la distancia de obtención de beneficios por encima del precio medio de entrada.
  - Se activa un trailing stop después de que el beneficio supera el umbral de activación y posteriormente el precio vuelve al nivel de seguimiento (se mueve en pasos).
- Las operaciones cortas utilizan reglas reflejadas con comparaciones de precios invertidas.
- Cuando *Solo una posición* está habilitado, el motor espera a que se cierre la exposición actual antes de realizar nuevas órdenes pendientes.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `StopLossPoints` | Distancia al stop-loss de protección en puntos (0 lo desactiva). |
| `TakeProfitPoints` | Distancia al objetivo de toma de ganancias en puntos (0 lo desactiva). |
| `MaxPositions` | Número máximo de posiciones activas y órdenes pendientes simultáneamente. |
| `MinStepPoints` | Distancia mínima entre el precio de entrada de las operaciones existentes y las nuevas órdenes pendientes. |
| `TrailingActivatePoints` | Umbral de beneficio que activa el trailing stop (0 desactiva el trailing). |
| `TrailingStopPoints` | Distancia entre el precio de mercado y el trailing stop una vez activado. |
| `TrailingStepPoints` | Mejora mínima del precio requerida para mover el trailing stop nuevamente. |
| `TradeMode` | Dirección permitida para nuevas órdenes pendientes: `Buy`, `Sell` o `BuySell`. |
| `PendingType` | Tipo de órdenes pendientes a realizar: `Stop` o `Limit`. |
| `PendingExpirationMinutes` | Duración de las órdenes pendientes en minutos (`0` las mantiene hasta que se completen o cancelen manualmente). |
| `PendingIndentPoints` | Compensación de la oferta/demanda actual utilizada para calcular los precios de las órdenes pendientes. |
| `PendingMaxSpreadPoints` | Spread máximo permitido entre oferta y solicitud para realizar órdenes pendientes (`0` desactiva el filtro). |
| `OnlyOnePosition` | Si `true`, impide abrir nuevas operaciones hasta que se cierre la posición actual. |
| `ReverseLevels` | Cambia la colocación de las órdenes de compra y venta para reflejar el modo inverso EA original. |
| `CandleType` | Marco de tiempo utilizado para activar la evaluación de la señal (diariamente por defecto). |

## Notas
- Las distancias de precios se expresan en puntos y se convierten automáticamente al tamaño del tick del instrumento.
- La estrategia se basa en StockSharp métodos auxiliares (`BuyStop`, `SellStop`, `BuyLimit`, `SellLimit`) para el registro de pedidos y utiliza `CancelActiveOrders` para restablecer el libro cada vez que se toma una nueva decisión.
- La lógica del trailing stop se evalúa en velas terminadas. Para el comportamiento de seguimiento dentro de la barra, utilice un `CandleType` más corto.
