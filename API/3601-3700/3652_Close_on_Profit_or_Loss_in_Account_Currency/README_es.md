# Cierre de ganancias o pérdidas en la moneda de la cuenta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia reproduce el asesor experto MetaTrader *Close_on_PROFIT_or_LOSS_inAccont_Currency*. Supervisa continuamente el capital de la cartera al que está vinculada la estrategia y, una vez que se alcanza un objetivo de ganancias configurado o un piso de reducción, liquida todas las posiciones abiertas y cancela todas las órdenes pendientes administradas por la estrategia. La clase se basa en el nivel alto de StockSharp API: una suscripción de vela proporciona el latido, `CancelActiveOrders()` elimina las órdenes de trabajo y `ClosePosition()` aplana la exposición a través de órdenes de mercado.

## como funciona

1. La estrategia sigue sondeando el valor actual (`Portfolio.CurrentValue`) cada vez que se cierra una vela de latido.
2. Si el capital es mayor o igual a **Cierre positivo**, la estrategia envía una solicitud de cierre completo.
3. Si el patrimonio es menor o igual a **Cierre Negativo**, se ejecuta la misma rutina de liquidación para limitar las pérdidas.
4. Durante la liquidación, la estrategia cancela todas las órdenes pendientes, envía órdenes de mercado para cerrar todas las posiciones activas y finalmente se detiene (reflejando la llamada `ExpertRemove()` del EA original).

> **Importante:** establezca los umbrales en la moneda de la cuenta. Para emular el comportamiento original, elija un valor de **Cierre positivo** encima del capital actual y un valor de **Cierre negativo** debajo de él; de lo contrario, la salida se activará inmediatamente después del inicio.

## Parámetros

| Nombre | Descripción | Predeterminado |
|------|-------------|---------|
| `PositiveClosureInAccountCurrency` | Nivel de patrimonio que desencadena una liquidación total cuando se excede. | `0` |
| `NegativeClosureInAccountCurrency` | Piso patrimonial que obliga a la liquidación cuando se alcanza. | `0` |
| `CandleType` | Marco de tiempo utilizado para las velas de latido que impulsan los controles de acciones. Redúzcalo para reacciones más rápidas. | `1 minute` |

## Notas

- `StartProtection()` se activa al inicio para copiar el comportamiento de seguridad original.
- La estrategia sólo interactúa con las posiciones y órdenes que gestiona; adjúntelo a la cartera que contiene las operaciones que desea proteger.
- No hay entradas separadas para diferenciales/deslizamientos porque StockSharp órdenes de mercado ya representan costos de ejecución específicos del conector.
