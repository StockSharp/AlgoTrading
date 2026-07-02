# Estrategia de plantilla de cuadrícula
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una versión StockSharp del asesor experto MetaTrader 4 **Grid_Template**. Construye una cuadrícula simétrica de cosas pendientes.
p órdenes alrededor de la oferta/demanda actual, lo que permite al operador conectar filtros de entrada personalizados o ejecutarlo como una plantilla de ruptura pura. una vez
Si todas las órdenes de la cuadrícula se han ejecutado o expirado, el motor prepara inmediatamente la siguiente cuadrícula. La implementación preserva t
La fórmula opcional de administración de dinero y la capacidad de eliminar automáticamente órdenes pendientes obsoletas después de un número configurable de
horas.

## Lógica comercial
- Suscríbase a las cotizaciones de Nivel 1 para realizar un seguimiento continuo de los mejores precios de oferta y demanda. No se requieren velas ni indicadores.
- Siempre que la cuenta no tenga ninguna posición abierta ni órdenes de estrategia activas, coloque `GridOrders` órdenes stop de compra por encima de la demanda y `G
Órdenes stop de venta de ridOrders por debajo de la oferta.
- El primer nivel de la red se compensa en `PriceDistancePips` del precio de mercado actual; cada nivel posterior agrega `GridStepPips` m
distancia del mineral.
- Cada entrada utiliza el mismo volumen fijo (o tamaño administrado por dinero) y las mismas distancias de stop-loss y take-profit expresadas en p
ips.
- Tan pronto como se ejecuta una orden pendiente, la estrategia registra las órdenes de protección correspondientes (stop loss y takeprofit) como
Órdenes stop/limit independientes. Estos heredan el mismo comentario para que sean fáciles de identificar.
- Si no se activa ninguna orden antes de que transcurra el temporizador de vencimiento, la plantilla cancela todas las órdenes pendientes en reposo y vuelve a armar el g.
deshacerse.

## gestión del dinero
- Cuando `UseMoneyManagement` está deshabilitado, todos los pedidos utilizan el parámetro fijo `StaticVolume`.
- Cuando está habilitado, el tamaño del lote se deriva de la fórmula de la plantilla original: `freeMargin * RiskPercent / 100000`, redondeado a la n
oreja `VolumeStep` y sujeta entre `VolumeMin` y `VolumeMax`. El valor actual de la cartera se utiliza como sustituto del de MT4.
margen libre.
- El volumen calculado está normalizado por la configuración del contrato de intercambio; si cae por debajo del tamaño mínimo comercializable, se establece en
cero, impidiendo el envío del pedido.

## Gestión de pedidos y riesgos.
- Las órdenes de parada de compra se colocan en `ask + PriceDistancePips + GridStepPips * level`. Las órdenes de parada de venta reflejan la lógica del sitio de oferta.
de.
- Las paradas de protección (`SellStop`/`BuyStop`) y los objetivos (`SellLimit`/`BuyLimit`) se registran solo después de completar una entrada pendiente.
. Esto imita el comportamiento de MT4 donde el stop loss y el takeprofit pertenecen al mismo ticket.
- `PendingExpirationHours` define cuánto tiempo permanecen activas las órdenes de entrada pendientes. Un valor cero los mantiene hasta que se llenen o se maquillen.
cancelado anualmente.
- Cuando la posición neta vuelve a cero, la estrategia también cancela cualquier orden de protección aún activa para garantizar un borrón y cuenta nueva.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `OrderComment` | Texto asignado a cada pedido generado por la grilla, que coincide con el comentario EA original. |
| `StaticVolume` | Tamaño de lote fijo utilizado cuando la administración del dinero está desactivada. |
| `UseMoneyManagement` | Habilita la rutina de dimensionamiento basada en equilibrio. |
| `RiskPercent` | Porcentaje utilizado por la fórmula de administración del dinero; ignorado cuando `UseMoneyManagement` es falso. |
| `TakeProfitPips` | Distancia de toma de ganancias aplicada a cada entrada de la red. |
| `StopLossPips` | Distancia de stop-loss aplicada a cada entrada a la red. |
| `PriceDistancePips` | Brecha inicial (en pips) entre el precio de mercado y la primera orden de la red. |
| `GridStepPips` | Distancia adicional (en pips) agregada entre niveles consecutivos de la cuadrícula. |
| `GridOrders` | Número de órdenes pendientes creadas a cada lado del precio. |
| `PendingExpirationHours` | Vida útil de la grilla pendiente antes de la cancelación. |

## Notas
- La plantilla no impone ningún filtro basado en indicadores; Los comerciantes pueden ampliar la clase y anular `TryPlaceGrid` para agregar personalizado.
m condiciones.
- Debido a que los objetivos y paradas de protección se implementan como órdenes separadas, la ejecución por parte del corredor puede diferir ligeramente de MT4 tik.
Gestión de stop-loss/take-profit estilo t, especialmente en rellenos parciales.
- Confirme siempre que el tamaño del pip inferido del intercambio (`PriceStep` y `Decimals`) coincida con el instrumento que se negocia.
antes de ejecutar la estrategia en una cuenta real.
