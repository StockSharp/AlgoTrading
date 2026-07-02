# Estrategia maestra de mercado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

`MarketMasterStrategy` es una conversión de alto nivel StockSharp del MetaTrader 4 asesor experto "Market Master" (`MQL/31326/MarketMaster EN.mq4`). El bot original combinaba una rica pila de indicadores con intrincadas reglas de administración del dinero, evitación de noticias y pirámides de órdenes de varias etapas. El port de C# se centra en el núcleo técnico determinista para que pueda ejecutarse en el motor controlado por eventos de StockSharp sin ningún servicio web externo. Todas las decisiones de los indicadores se calculan en el marco temporal de negociación mediante una única suscripción de vela, de acuerdo con las directrices del repositorio.

## Indicadores básicos

La estrategia vincula los siguientes indicadores StockSharp a la serie de velas comerciales:

- **AverageTrueRange (ATR)**: se mantienen dos instancias. El primero rastrea las condiciones de entrada primarias, el segundo refleja la "cobertura" MT4 ATR que se usó para las posiciones de recuperación.
- **MoneyFlowIndex (MFI)**: mide el flujo de precios ajustado por volumen para detectar oscilaciones de acumulación o distribución.
- **BullsPower / BearsPower**: replica los filtros MT4 `iBullsPower` y `iBearsPower` que requerían dominio alcista/bajista antes de realizar operaciones.
- **StochasticOscillator**: ofrece líneas `%K` y `%D`. La conversión respeta las longitudes originales del oscilador y permite al usuario activar o desactivar el filtro.
- **ParabolicSar**: se utilizaron dos períodos de tiempo en MetaTrader. El puerto StockSharp mantiene dos indicadores SAR independientes (primario y de confirmación) cuyos pasos reflejan las entradas del asesor experto.

Todos los indicadores se calientan automáticamente con StockSharp. La estrategia no accede al historial del indicador a través de `GetValue`; en cambio, almacena los valores anteriores dentro de campos privados (`_prevAtr`, `_prevMfi`, `_prevStochasticMain`, etc.) según lo requieren las reglas de conversión.

## Lógica de señal

El experto MQL definió dos familias de entradas principales ("ZERO" y "MA"). Comparten filtros ATR/MFI/Bulls/Bears idénticos, pero difieren en la confirmación del oscilador. La versión StockSharp expone la rama "MA" más rica porque es la más restrictiva y, por lo tanto, la más cercana a las condiciones comerciales reales. Una señal larga se confirma cuando se cumple todo lo siguiente en una vela terminada:

1. ATR está aumentando en relación con la vela anterior (ya sea la ATR primaria o la cobertura ATR dependiendo de si ya existe una posición).
2. El IMF está aumentando y el Bears Power es positivo, lo que indica una presión alcista.
3. El oscilador Stochastic está habilitado y `%K` está por encima de `%D`, con tendencia alcista, mientras que `%K` permanece por debajo del techo de sobrecompra configurable (`StochasticBuyLevel`).
4. Los filtros Parabolic SAR están habilitados y la vela se cierra por encima de ambos valores SAR.
5. El volumen de velas actual cumple con el umbral configurado (`MinVolume` o `MinHedgeVolume`).

Las señales cortas reflejan la lógica larga con un MFI decreciente, un poder alcista negativo, valores `%K` por debajo de `%D` y SAR por encima del precio. Los controles de volumen evitan el comercio durante mercados reducidos, replicando las llamadas `iVolume` de MT4.

## Gestión de Puestos

- **Volumen automático**: el EA original ofrecía un bloque de tamaño de posición basado en el equilibrio. `CalculateBaseVolume` sigue el mismo espíritu al escalar el volumen de la orden con `RiskMultiplier` respetando al mismo tiempo las restricciones del instrumento `VolumeStep`, `MinVolume` y `MaxVolume`.
- **Pirámide**: cuando `AllowSameSignalEntries` es `true`, los pedidos adicionales reutilizan el volumen base multiplicado por `VolumeMultiplier`. Debido a que las estrategias StockSharp funcionan con posiciones netas, la piramidación aumenta la exposición neta larga o neta corta en lugar de abrir tickets paralelos.
- **Señales opuestas**: `AllowOppositeEntries` controla si una reversión detectada cierra inmediatamente la posición actual y, opcionalmente, abre una operación en la nueva dirección. Cuando está deshabilitada, la estrategia sale pero espera una nueva señal antes de volver a ingresar, imitando la opción "Sin señal opuesta" en la interfaz MT4.
- **Stop-loss**: la entrada MT4 `StopLoss` se expone como `StopLossPoints`. Si el instrumento proporciona un `PriceStep`, el valor se convierte en StockSharp órdenes de protección a través de `StartProtection`.
- **Horario de negociación**: `UseTradingWindow`, `TradingStart`, `TradingEnd`, `UseTradingBreak`, `BreakStart` y `BreakEnd` reproducen la ventana de apertura y la pausa intradiaria del experto fuente. Las comparaciones horarias se realizan en la zona horaria del intercambio transmitida por los mensajes de vela entrantes.

## Diferencias versus la versión MetaTrader

- **Filtros de noticias**: el robot MT4 descargó datos del calendario económico de Investing.com y DailyFX. La conversión omite todas las llamadas de red y las reemplaza con control manual sobre la ventana de negociación. Para comportamientos sensibles a las noticias, ajuste los parámetros de tiempo o pausar la estrategia externamente.
- **Verificaciones del historial de pedidos**: funciones como `OrdersHistoryTotal()` y la lógica de "abrir de nuevo" estaban estrechamente acopladas al modelo de ticket de MetaTrader. StockSharp funciona con una posición neta, por lo que el puerto simplemente permite el reingreso cuando el filtro de dirección vuelve a ser válido.
- **Órdenes de recuperación**: el código original gestionaba múltiples Números Mágicos y etiquetas de comentarios. El puerto mantiene la lógica multiplicadora (`VolumeMultiplier`) pero cada orden adicional modifica la posición neta única.
- **Trailing stop**: el bloque `TrailingStop`/`TrailingStep` de MetaTrader se basó en la modificación de orden asincrónica. Los usuarios de StockSharp pueden ampliar la estrategia suscribiéndose a eventos de `PositionChanged` o habilitando opciones de seguimiento en `StartProtection`, pero la conversión básica se centra en la paridad de la señal.

## Parámetros

| Propiedad | Predeterminado | Descripción |
| --- | --- | --- |
| `OrderVolume` | `1` | Tamaño de pedido base cuando el volumen automático está deshabilitado. |
| `UseAutoVolume` | `true` | Habilite el escalado de volumen basado en riesgos. |
| `RiskMultiplier` | `10` | Porcentaje del saldo de la cartera utilizado en el cálculo del volumen automático (espejos `Risk_Multiplier`). |
| `VolumeMultiplier` | `2` | Factor de piramidal para entradas adicionales (`KLot`). |
| `MinVolume` | `3000` | Volumen mínimo de vela para la primera entrada (`MinVol`). |
| `MinHedgeVolume` | `3000` | Umbral de volumen para operaciones complementarias (`MinVolH`). |
| `AtrPeriod` / `AtrHedgePeriod` | `14` | ATR longitudes para los filtros de base y seto. |
| `MfiPeriod` | `14` | Período de las IMF. |
| `BullBearPeriod` | `14` | Período de poder alcistas y bajistas. |
| `StochasticKPeriod` / `StochasticDPeriod` / `StochasticSlowing` | `5 / 3 / 3` | Stochastic configuración del oscilador. |
| `StochasticBuyLevel` / `StochasticSellLevel` | `60 / 40` | Umbrales del oscilador (`StoBuy` y `StoSell`). |
| `UseStochasticFilter`, `UsePsarFilter`, `UsePsarConfirmation` | `true` | Alterna para confirmaciones basadas en indicadores. |
| `PsarStep` / `PsarMaxStep` / `PsarConfirmStep` / `PsarConfirmMaxStep` | `0.02 / 0.2 / 0.02 / 0.2` | SAR aceleraciones y límites. |
| `AllowSameSignalEntries` | `false` | Habilite la pirámide en señales idénticas. |
| `AllowOppositeEntries` | `true` | Permitir operaciones de reversión inmediata. |
| `UseTradingWindow` | `false` | Restrinja el comercio a un intervalo de tiempo. |
| `TradingStart` / `TradingEnd` | `06:00 / 18:00` | Ventana de negociación diaria. |
| `UseTradingBreak` | `false` | Habilite un breve descanso intradiario. |
| `BreakStart` / `BreakEnd` | `06:00:01 / 06:00:02` | Rompe límites (coincide con los valores predeterminados de MT4). |
| `StopLossPoints` | `0` | Tope de protección opcional en puntos de instrumentos. |
| `CandleType` | `15m TimeFrame` | Serie de velas utilizadas para todos los indicadores. |

## Notas de uso

1. Adjunte la estrategia a un valor y una cartera en StockSharp Designer o en código, luego iníciela durante las horas de preparación para permitir que se formen todos los indicadores.
2. Si necesita confirmación de varios períodos de tiempo, ajuste `CandleType` y la configuración de SAR en consecuencia. La estrategia se suscribe a una única vela y vincula todos los indicadores a través de `Bind`, por lo que no es necesario el registro manual del indicador.
3. Utilice el registro StockSharp (`LogInfo`, `LogWarning`) para depurar si amplía el código. La conversión mantiene la gestión del estado interno simple para que se puedan conectar fácilmente módulos adicionales (por ejemplo, protección de seguimiento).
4. La estrategia se basa en la posición neta. Si planea modelar un comportamiento de ticket individual similar a MetaTrader, incluya la estrategia dentro de un enrutador de seguridad múltiple que rastree tickets sintéticos.

## Ampliando el puerto

- Implemente una lógica de salida personalizada anulando `OnNewMyTrade` o suscribiéndose a `PositionChanged`.
- Agregue integración al calendario económico introduciendo un componente externo que alterna `UseTradingWindow` o llama a `Stop()` cuando se acercan eventos de alto impacto.
- Para visualizar la señal, llame a `CreateChartArea()` y `DrawIndicator()` en `OnStarted`; la conversión deja esos ganchos vacíos para mayor claridad.

El código cumple totalmente con las pautas del repositorio: utiliza sangría de tabulación, suscripciones `Bind` de alto nivel, evita referencias inversas de indicadores y expone todas las entradas configurables a través de objetos `StrategyParam`.
