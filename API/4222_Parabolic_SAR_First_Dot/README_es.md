# Parabolic SAR Estrategia del primer punto
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Parabolic SAR estrategia del primer punto** es la StockSharp conversión de alto nivel del MetaTrader asesor experto `pSAR_bug_4` de la carpeta `MQL/9954`. El sistema reacciona al primer punto del Parabolic SAR que aparece en el lado opuesto del precio. Cuando el SAR cae por debajo del cierre, se abre una operación larga; cuando el SAR salta por encima del cierre, se ejecuta una operación corta. Cada posición está protegida con distancias fijas de stop-loss y take-profit expresadas en Parabolic SAR "puntos", al igual que en la versión original MQL.

## Lógica de trading
1. **Preparación de datos e indicadores**. La estrategia se suscribe a un tipo de vela configurable (velas de 15 minutos de forma predeterminada) y vincula un indicador Parabolic SAR con un paso de aceleración definido por el usuario y una aceleración máxima.
2. **Seguimiento del estado**. En la primera vela completa, la estrategia recuerda si SAR está por encima o por debajo del cierre. Las velas posteriores comparan la nueva posición SAR con el estado anterior.
3. **Reglas de entrada**.
   - **Entrada larga**: el SAR cambia desde arriba del cierre hasta debajo del cierre. Cualquier posición corta existente se cierra y se abre en el mercado una nueva posición larga con el volumen configurado.
   - **Entrada corta**: el SAR cambia de debajo del cierre a encima del cierre. Cualquier posición larga existente se cierra antes de abrir una nueva posición corta.
4. **Órdenes de protección**. Inmediatamente después de la entrada, la estrategia almacena los niveles de stop-loss y take-profit calculados a partir del cierre de la vela multiplicando `StopLossPoints` o `TakeProfitPoints` por el valor `PriceStep`. Si `UseStopMultiplier` está habilitado (comportamiento predeterminado copiado de MetaTrader), la distancia se multiplica por 10 para tener en cuenta los corredores que cotizan con pips fraccionarios.
5. **Reglas de salida**. En cada vela terminada, la estrategia compara el máximo y el mínimo con los niveles almacenados de stop-loss y take-profit. Si el máximo o el mínimo superan el nivel, la posición se cierra en el mercado. Cuando llega una señal SAR opuesta, la posición también se invierte enviando una orden de tamaño para nivelar la exposición actual y abrir la nueva operación.

## Gestión del riesgo
- Las distancias de stop-loss y take-profit se recalculan para cada nueva posición.
- El código realiza un respaldo conservador: cuando el valor no proporciona un incremento de precio, se utiliza un valor de `0.0001` para evitar distancias cero.
- Todas las decisiones comerciales utilizan el asistente `IsFormedAndOnlineAndAllowTrading()` para garantizar que la suscripción esté activa y activa.

## Parámetros
| Nombre | Predeterminado | Descripción |
| --- | --- | --- |
| `TradeVolume` | `0.1` | Volumen de pedidos utilizado para nuevas posiciones. El parámetro también actualiza la propiedad base `Strategy.Volume`. |
| `StopLossPoints` | `90` | Distancia de stop-loss expresada en Parabolic SAR puntos. El valor se multiplica por la seguridad `PriceStep` (y ​​opcionalmente por 10 cuando `UseStopMultiplier` es verdadero). |
| `TakeProfitPoints` | `20` | Distancia de obtención de beneficios en Parabolic SAR puntos convertidos a través del paso de precio. |
| `UseStopMultiplier` | `true` | Si está habilitado, multiplica las distancias de stop-loss y take-profit por 10 para imitar el cambio `StopMult` del experto MetaTrader. |
| `SarAccelerationStep` | `0.02` | Factor de aceleración inicial suministrado al indicador Parabolic SAR. |
| `SarAccelerationMax` | `0.2` | Factor de aceleración máximo para el indicador Parabolic SAR. |
| `CandleType` | `15m time-frame` | Tipo de vela utilizado para los cálculos de indicadores y señales. |

## Notas sobre la conversión
- MetaTrader las órdenes de limitación de pérdidas y toma de ganancias eran órdenes de protección del lado del corredor. StockSharp los reproduce monitoreando los máximos y mínimos de las velas y enviando salidas del mercado cuando se cruzan los umbrales.
- El experto MetaTrader multiplicó las distancias de parada por diez siempre que `StopMult` fuera cierto para mejorar la compatibilidad con los corredores que cotizan con pips fraccionarios. El parámetro `UseStopMultiplier` implementa el mismo comportamiento.
- La conversión utiliza API de alto nivel de StockSharp (`SubscribeCandles`, `Bind`, `BuyMarket`, `SellMarket`) según lo exigen las pautas del proyecto. Aún no se proporciona ninguna versión adicional de Python que coincida con la solicitud de la tarea.
