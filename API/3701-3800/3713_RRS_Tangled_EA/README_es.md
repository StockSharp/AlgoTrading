# Estrategia RRS Enredada EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia RRS Tangled EA** es una versión StockSharp del MetaTrader 4 asesor experto "RRS Tangled EA". El sistema original elige aleatoriamente la dirección y el símbolo de la operación, al tiempo que limita el número de órdenes simultáneas y protege las ganancias flotantes mediante paradas dinámicas y límites de riesgo estrictos. La versión convertida se centra en el instrumento seleccionado actualmente y reproduce la entrada aleatoria, el seguimiento y el comportamiento de gestión de riesgos utilizando el StockSharp API de alto nivel.

## Lógica principal
1. Suscríbase a la serie de velas configuradas y espere las velas completas.
2. En cada barra:
   - Actualice los niveles de trailing stop para cestas largas y cortas existentes.
   - Verifique las distancias de stop-loss y take-profit utilizando máximos y mínimos de velas.
   - Evaluar el beneficio flotante de todas las entradas abiertas; cerrar todo si supera el umbral de dinero en riesgo.
   - Si se permite el comercio, el diferencial es aceptable y el número de entradas está por debajo del límite, extraiga un número entero aleatorio en `[0, 3]`.
   - Abra un nuevo largo cuando el valor aleatorio sea `1`, o un nuevo corto cuando el valor sea `2`, utilizando un volumen aleatorio entre los límites configurados.
3. Los trailingstops siguen la mejor oferta/demanda una vez que el precio se mueve a lo largo de la distancia de activación, asegurando ganancias si el precio retrocede a lo largo de la brecha de seguimiento.
4. La gestión de riesgos puede funcionar en modo de dinero fijo o como porcentaje del saldo de la cuenta corriente. Cuando la pérdida flotante excede la cantidad configurada, todas las posiciones se aplanan inmediatamente.

## Parámetros
| Nombre | Descripción |
|------|-------------|
| `MinVolume` | Límite inferior para el volumen comercial generado aleatoriamente. |
| `MaxVolume` | Límite superior para el volumen de comercio aleatorio. |
| `TakeProfitPips` | Distancia objetivo en pips, aplicada al precio medio de entrada de la cesta. |
| `StopLossPips` | Distancia de parada de protección en pips, medida a partir del precio de entrada promedio. |
| `TrailingStartPips` | Distancia de beneficio necesaria antes de que se active la lógica de seguimiento. |
| `TrailingGapPips` | Se mantiene la brecha entre el trailing stop y el mejor precio de oferta/demanda. |
| `MaxSpreadPips` | Spread máximo permitido antes de abrir una nueva entrada aleatoria. |
| `MaxOpenTrades` | Número máximo de entradas simultáneas en ambas direcciones. |
| `RiskManagementMode` | Cambia entre manejo de riesgo de dinero fijo y porcentaje de saldo. |
| `RiskAmount` | Cantidad de riesgo (moneda o porcentaje) monitoreado contra PnL flotante. |
| `TradeComment` | Comentario opcional para contabilidad, mantenido por compatibilidad con la fuente EA. |
| `Notes` | Texto informativo que se muestra dentro de la cadena de estado de la estrategia. |
| `CandleType` | Serie de velas utilizadas para la toma de decisiones. |

## Diferencias con la versión MQL
- Las operaciones se ejecutan en el instrumento asignado a la estrategia en lugar de seleccionar símbolos aleatoriamente de la observación del mercado MetaTrader. Esto mantiene la implementación compatible con las estrategias de seguridad única de StockSharp.
- La gestión de pedidos se realiza en cestas largas/cortas agregadas, reflejando cómo el EA original agrupaba posiciones con los mismos números mágicos.
- El control de diferencial se basa en la mejor oferta/demanda más reciente del libro de pedidos en lugar de las llamadas `MarketInfo` de MetaTrader.

## Notas de uso
- Asegúrese de que el corredor o simulador conectado proporcione cotizaciones tanto de oferta como de demanda para que los cálculos de diferencial y seguimiento sigan siendo precisos.
- Configure `MinVolume` y `MaxVolume` dentro del rango de volumen permitido del instrumento. La estrategia ajusta automáticamente el volumen aleatorio al paso y los límites de volumen del símbolo.
- La lógica de gestión de riesgos cierra *todas* las operaciones inmediatamente una vez que la pérdida flotante supera el umbral configurado; no se abren nuevas posiciones hasta la siguiente vela.
