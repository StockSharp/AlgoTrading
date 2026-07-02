# Estrategia de Gann Grid
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto original **Gann Grid** de `MQL/25065/Gann Grid.mq4` a la API de alto nivel de StockSharp. El script original mezclaba objetos de gráfico manuales con filtros de múltiples marcos temporales; la versión en C# mantiene el flujo de trabajo general mientras reemplaza los datos derivados de gráficos con lógica impulsada por indicadores que puede ejecutarse de forma desatendida.

## Lógica de trading

1. **Grid de Gann sintético** – el máximo más alto y el mínimo más bajo durante `AnchorPeriod` velas aproximan los niveles de precio que se dibujaban manualmente en MetaTrader. Una ruptura por encima del máximo desencadena configuraciones largas, una ruptura por debajo del mínimo desencadena cortos.
2. **Confirmación de tendencia** – las medias móviles ponderadas linealmente rápida y lenta en el marco temporal superior (`TrendCandleType`) deben estar de acuerdo con la dirección de la ruptura.
3. **Filtro de Momentum** – la distancia porcentual entre el indicador de Momentum y el precio actual (también en el marco temporal superior) necesita exceder `MomentumThreshold` para asegurar que haya suficiente aceleración.
4. **Confirmación MACD** – un flujo de velas separado (`MacdCandleType`) alimenta un MACD (12/26/9 por defecto). La línea MACD debe estar en el mismo lado de cero y de la línea de señal que la dirección del trade.
5. **Gestión de riesgo** – se aplican compensaciones simétricas de stop-loss y take-profit desde el precio de entrada. Los módulos opcionales de break-even y trailing reproducen los bloques de protección de capital de la implementación MQL.

Solo se procesan velas terminadas para coincidir con las verificaciones originales de "nueva barra".

## Diferencias versus la versión MQL

- El código MetaTrader esperaba un objeto `GANNGRID` dibujado manualmente. El port lo reemplaza con indicadores de máximo/mínimo continuo, haciendo la lógica determinista para pruebas automatizadas.
- El Momentum en MetaTrader está centrado alrededor de 100. El `Momentum` de StockSharp produce una diferencia de precio, por lo tanto la estrategia lo convierte en un porcentaje del cierre actual antes de compararlo con `MomentumThreshold`.
- Las notificaciones (correo electrónico, push) y las operaciones gráficas del script MQL se omiten.
- La gestión de riesgo usa salidas de mercado en lugar de modificar órdenes existentes, porque las estrategias de StockSharp gestionan posiciones en lugar de órdenes a nivel de terminal.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Marco temporal de 5 minutos | Velas primarias que definen las rupturas. |
| `TrendCandleType` | `DataType` | Marco temporal de 15 minutos | Marco temporal superior usado para LWMA y filtros de Momentum. |
| `MacdCandleType` | `DataType` | Marco temporal de 1 día | Flujo de velas que alimenta el filtro de confirmación MACD. |
| `FastMaPeriod` | `int` | 6 | Longitud LWMA rápida en el marco temporal superior. |
| `SlowMaPeriod` | `int` | 85 | Longitud LWMA lenta en el marco temporal superior. |
| `MomentumPeriod` | `int` | 14 | Longitud de retroceso del Momentum. |
| `MomentumThreshold` | `decimal` | 0.3 | Desviación mínima de Momentum en porcentaje requerida para operar. |
| `AnchorPeriod` | `int` | 100 | Número de velas primarias que forman el grid de Gann sintético. |
| `TakeProfitOffset` | `decimal` | 0.005 | Distancia absoluta de take-profit desde el precio de entrada. |
| `StopLossOffset` | `decimal` | 0.002 | Distancia absoluta de stop-loss desde el precio de entrada. |
| `EnableTrailing` | `bool` | `true` | Habilita la gestión del trailing stop. |
| `TrailingActivation` | `decimal` | 0.003 | Beneficio requerido antes de que el trailing stop comience a seguir el precio. |
| `TrailingStep` | `decimal` | 0.0015 | Distancia entre el máximo local y el trailing stop. |
| `EnableBreakEven` | `bool` | `true` | Activa la lógica de movimiento al break-even. |
| `BreakEvenTrigger` | `decimal` | 0.0025 | Beneficio necesario antes de armar el break-even. |
| `BreakEvenOffset` | `decimal` | 0.0 | Compensación aplicada al precio de entrada al cerrar en break-even. |
| `MacdFastPeriod` | `int` | 12 | Longitud EMA rápida dentro del MACD. |
| `MacdSlowPeriod` | `int` | 26 | Longitud EMA lenta dentro del MACD. |
| `MacdSignalPeriod` | `int` | 9 | Longitud EMA de señal dentro del MACD. |

Todas las compensaciones son distancias de precio absolutas. Ajústalas para que coincidan con el tamaño de tick del símbolo (por ejemplo, 0.001 ≈ 10 puntos en una cotización FX de 5 dígitos).

## Cómo usar

1. Adjunte la estrategia a un valor y configure los tipos de velas. Es posible usar el mismo tipo de vela para múltiples filtros si se desea un único marco temporal.
2. Ajuste `AnchorPeriod` y las compensaciones de precio para que coincidan con la volatilidad del instrumento.
3. Habilite o deshabilite el break-even/trailing según su política de riesgo.
4. Inicie la estrategia; se suscribe automáticamente a los flujos de velas necesarios y gestiona posiciones con órdenes de mercado.

## Archivos

- `CS/GannGridStrategy.cs` – implementación de la estrategia.
- `README.md` – esta documentación.
- `README_ru.md` – descripción en ruso.
- `README_zh.md` – descripción en chino.
