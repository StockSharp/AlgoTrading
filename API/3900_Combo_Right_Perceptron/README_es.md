# Estrategia combinada del perceptrón derecho
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una fiel versión StockSharp del asesor experto MetaTrader **Combo_Right.mq4**. Combina un filtro de impulso de índice de canal de productos básicos (CCI) con tres perceptrones que analizan el impulso del precio de apertura en pasos de barra configurables. Dependiendo de `PassMode`, los perceptrones pueden anular la señal CCI e indicar al supervisor que abra posiciones largas o cortas con sus parámetros de riesgo dedicados.

## Lógica de trading

1. Suscríbase al tipo de vela configurado y calcule el CCI sobre los precios de apertura. La última vela completa proporciona tanto el precio de cierre como los valores de apertura históricos para las entradas del perceptrón.
2. Mantenga un búfer circular de precios de apertura para que los perceptrones puedan acceder a la apertura de las barras `period`, `2*period`, `3*period` y `4*period` sin depender de los captadores del historial del indicador.
3. Cuando llegue una vela terminada:
   - Evalúe el valor CCI. Esto actúa como la señal predeterminada (`> 0` = larga, `< 0` = corta) con las distancias de protección base (`TakeProfit1` / `StopLoss1`).
   - Dependiendo de `PassMode`, calcula uno o varios perceptrones. Cada perceptrón utiliza ponderaciones derivadas de las entradas originales MQL (`X** - 100`) y las diferencias entre el cierre más reciente y las aperturas históricas.
   - Si se cumple una condición del perceptrón, anula la señal predeterminada y asigna sus propias distancias de stop-loss/take-profit antes de enviar cualquier orden.
4. Cancele las órdenes de trabajo, aplane la exposición opuesta y abra la nueva posición usando el `TradeVolume` configurado. Después de enviar la orden de mercado, llame a `SetTakeProfit` y `SetStopLoss` con las compensaciones calculadas para que las órdenes de protección reflejen la rama activa del perceptrón.

### Modos de pase

- **Pase 1**: solo se considera el valor CCI. La señal es proporcional al último valor del indicador.
- **Pase 2**: si el primer perceptrón (`Perceptron1Period`, `X12…X42`) produce un resultado negativo, la estrategia abre inmediatamente una operación corta con el segundo perfil de riesgo. De lo contrario, vuelve al resultado CCI.
- **Pase 3**: si el segundo perceptrón es positivo, la estrategia abre una operación larga con el tercer perfil de riesgo. De lo contrario, se basa en la salida CCI.
- **Pase 4**: primero verifique el tercer perceptrón. Un valor positivo requiere que el segundo perceptrón también sea positivo para permitir una entrada larga con el perfil de riesgo alcista. Si el tercer perceptrón es negativo y el primer perceptrón está por debajo de cero, el supervisor abre una posición corta con el perfil de riesgo bajista. Si ninguna de las ramas se activa, se utiliza la salida CCI.

En todos los modos, la estrategia ignora las señales hasta que se recolectan suficientes velas para alimentar el paso más profundo del perceptrón.

## Gestión del riesgo

Cada entrada calcula nuevas compensaciones de precios basadas en el símbolo `PriceStep`. Si el instrumento no proporciona un paso, la distancia del punto bruto se utiliza tal cual. `SetTakeProfit` y `SetStopLoss` reciben las compensaciones deseadas junto con la posición neta resultante para que los soportes protectores permanezcan sincronizados con la exposición actual.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `TakeProfit1`, `StopLoss1` | `decimal` | 50 / 50 | Distancias de pérdidas y ganancias (en puntos) cuando se utiliza la salida CCI. |
| `CciPeriod` | `int` | 10 | Período del CCI calculado sobre los precios de apertura. |
| `X12`, `X22`, `X32`, `X42` | `int` | 100 | Pesos brutos para el perceptrón bajista; la estrategia resta internamente 100 como en el código original. |
| `TakeProfit2`, `StopLoss2` | `decimal` | 50 / 50 | Distancias de riesgo (puntos) aplicadas cuando se activa el perceptrón bajista. |
| `Perceptron1Period` | `int` | 20 | Camina entre muestras para el perceptrón bajista (en barras). |
| `X13`, `X23`, `X33`, `X43` | `int` | 100 | Pesos brutos para el perceptrón alcista. |
| `TakeProfit3`, `StopLoss3` | `decimal` | 50 / 50 | Distancias de riesgo (puntos) aplicadas cuando se activa el perceptrón alcista. |
| `Perceptron2Period` | `int` | 20 | Paso entre muestras para el perceptrón alcista (en barras). |
| `X14`, `X24`, `X34`, `X44` | `int` | 100 | Pesos brutos para el perceptrón de confirmación utilizado en `PassMode = 4`. |
| `Perceptron3Period` | `int` | 20 | Camine entre muestras para el perceptrón de confirmación (en barras). |
| `PassMode` | `int` | 1 | Modo supervisor (1–4) que reproduce la lógica de ramificación del experto MQL. |
| `TradeVolume` | `decimal` | 0,01 | Volumen utilizado para nuevas entradas al mercado. La exposición opuesta se cierra antes de entrar. |
| `CandleType` | `DataType` | M1 | Serie de velas que alimenta las entradas del CCI y del perceptrón. |

## Notas

- La implementación espera intencionalmente hasta que todos los perceptrones tengan suficientes precios de apertura históricos antes de negociar, evitando problemas relacionados con la matriz que estaban implícitos en MetaTrader.
- Los valores de los indicadores nunca se recuperan mediante acceso aleatorio. En cambio, el historial requerido se almacena en un buffer circular que cumple con las directrices del proyecto.
- Todos los comentarios y la documentación se mantienen en inglés para cumplir con los requisitos del repositorio.
