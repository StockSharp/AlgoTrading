# Estrategia BuySellOnYourPrice
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
- Convierte el asesor experto MetaTrader **BuySellonYourPrice.mq5** (id 35391) en la API de alto nivel de StockSharp.
- Envía exactamente una orden al inicio, coincidiendo con la lógica original que no requiere órdenes ni posiciones activas.
- Admite entradas de mercado, límite y parada con niveles opcionales de parada de pérdidas y toma de ganancias expresados como precios absolutos.
- Configura automáticamente StockSharp órdenes de protección cuando se pueden derivar distancias válidas de stop-loss/take-profit a partir de los niveles de precios proporcionados.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Mode` | Tipo de orden a enviar (Ninguno, Comprar, Vender, BuyLimit, SellLimit, BuyStop, SellStop). | `None` |
| `OrderVolume` | Volumen del pedido generado. | `1` |
| `EntryPrice` | Precio utilizado para órdenes pendientes; ignorado para las órdenes de mercado. | `0` |
| `StopLossPrice` | Nivel de precio absoluto del stop-loss. | `0` |
| `TakeProfitPrice` | Nivel de precio absoluto para la toma de ganancias. | `0` |

## Lógica de trading
1. Cuando la estrategia comienza comprueba que:
   - Se selecciona un `Mode` válido diferente de `None`.
   - `OrderVolume` es positivo.
   - No hay ninguna posición actual ni órdenes activas. Si cualquiera de ellos está presente, el pedido no se envía (igual que `OrdersTotal()==0` y `PositionsTotal()==0` verificar en MQL).
2. El precio de entrada se resuelve:
   - Los modos de mercado utilizan la mejor oferta/demanda, retrocediendo al último precio o `EntryPrice` cuando aún no hay datos de mercado disponibles.
   - Los modos pendientes requieren `EntryPrice > 0`.
3. Las distancias de protección se derivan de los niveles de stop-loss y take-profit especificados. Solo se pasan distancias positivas válidas a `StartProtection` para emular los parámetros EA.
4. El tipo de orden seleccionado se envía (`BuyMarket`, `SellLimit`, `BuyStop`, etc.) exactamente una vez y se generan registros informativos para reflejar la acción.

## Diferencias con el original EA
- El registro se realiza a través de `AddInfoLog` en lugar de `Print`.
- Las órdenes de protección se registran vía `StartProtection` cuando tanto el precio de entrada como el stop-loss/take-profit permiten calcular una distancia positiva.
- La resolución de precios de mercado utiliza datos actuales de Nivel 1 (`BestBid`, `BestAsk`, `LastPrice`) y pospone el envío de pedidos si aún no hay una cotización disponible.

## Notas de uso
- Asigne el valor deseado antes de comenzar la estrategia y asegúrese de que los datos de Nivel 1 estén disponibles para las órdenes de mercado.
- Establezca `EntryPrice`, `StopLossPrice` y `TakeProfitPrice` en términos absolutos cuando utilice órdenes pendientes.
- Deje `Mode` como `None` para deshabilitar el comercio sin eliminar la estrategia del entorno.
