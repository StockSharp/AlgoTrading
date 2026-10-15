# Estrategia de cuadrícula de límites pendiente (MQL/8147 Conversión)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de cuadrícula de límites pendiente** reproduce el comportamiento del experto MetaTrader
almacenado en `MQL/8147`. La estrategia construye una cuadrícula simétrica de órdenes límite pendientes.
alrededor de los precios actuales de oferta y demanda. Mantiene la red activa mientras las ganancias flotan.
permanece dentro de un objetivo de ganancias configurado y un umbral de reducción. Cuando uno de los
se superan los umbrales, se cancelan todas las órdenes, se aplanan las posiciones abiertas y
la red se reconstruye utilizando el patrimonio de la nueva cuenta como base de referencia.

## Lógica de trading

1. Suscríbase a los datos de nivel uno para realizar un seguimiento de los mejores precios de oferta y demanda.
2. Capture el capital de la cuenta la primera vez que se reciban datos en vivo y guárdelos como
la línea base de la sesión.
3. Coloque `LevelsPerSide` límites de venta por encima del mercado y la misma cantidad de compras
límites por debajo del mercado. La distancia entre los niveles de la cuadrícula está controlada por
`GridStepPoints` convertido al paso del precio del instrumento.
4. Mantenga las órdenes pendientes sin volver a emitir nuevas cuando se ejecuten. el
La cuadrícula se recrea solo después de un reinicio completo.
5. Supervise continuamente el PnL flotante:
   - Si la ganancia alcanza `ProfitTargetCurrency`, cierre toda exposición y reiníciela.
   - Si la reducción excede `MaxDrawdownCurrency`, aplana el libro y reinícialo.
6. Después de cada reinicio, la equidad de referencia se captura nuevamente y se reconstruye la red.
utilizando la instantánea de oferta/demanda más reciente.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `ProfitTargetCurrency` | Beneficio neto (en la moneda de la cuenta) que desencadena un reinicio completo de la red. |
| `MaxDrawdownCurrency` | Pérdida flotante máxima tolerada antes de que se cierre toda exposición. |
| `GridStepPoints` | Distancia entre niveles consecutivos de la grilla expresada en puntos de corredor. |
| `LevelsPerSide` | Número de órdenes pendientes creadas por encima y por debajo del mercado. |
| `OrderVolume` | Volumen asignado a cada orden límite pendiente. |

## Gestión del riesgo

La estrategia no adjunta paradas ni objetivos por orden. En cambio, supervisa el
pérdidas y ganancias agregadas. El ayudante `RequestFlatten` cancela los pedidos pendientes y
utiliza órdenes de mercado (a través de `ClosePosition`) para eliminar cualquier exposición abierta. Después del
Cuando se completa el aplanamiento, el estado de la red y la equidad de referencia se restablecen antes de colocar
nuevos pedidos.

## Notas

- Los precios están normalizados a través de `Security.ShrinkPrice` para respetar el cambio.
paso de precio.
- El valor "Punto" MetaTrader se emula analizando el instrumento `PriceStep`
para hacer coincidir cotizaciones de cuatro y cinco dígitos.
- La estrategia evita reenviar los pedidos de la grilla una vez realizados, imitando el
experto original que se basaba en variables de bandera para mantener cada nivel único hasta
se produce un reinicio manual o automático.
