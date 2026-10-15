# Estrategia de Cidomo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Sistema de Rompimento convertido del asesor experto de MetaTrader 5 "Cidomo". La estrategia espera una nueva vela en el marco temporal configurado, mide el rango de trading reciente y coloca órdenes stop pareadas por encima y por debajo de ese rango. Gestiona el riesgo con niveles clásicos de stop-loss/take-profit, un trailing stop opcional y dos modos de gestión de capital (volumen fijo o riesgo porcentual).

## Cómo funciona

1. En cada vela terminada de `CandleType`, recolectar los últimos máximos y mínimos de `BarsCount` para definir el canal de corto plazo.
2. Colocar una orden buy stop en `highest + IndentPips` y una sell stop en `lowest - IndentPips` (ambos valores expresados en pips y convertidos a precios absolutos).
3. Cuando se activa una orden stop, la orden pendiente opuesta se cancela inmediatamente.
4. Para una posición abierta, la estrategia realiza un seguimiento de:
   - Stop-loss inicial (`StopLossPips`) y take-profit (`TakeProfitPips`).
   - Un trailing stop escalonado (`TrailingStopPips` / `TrailingStepPips`). El stop se mueve solo después de que el precio avanza al menos `TrailingStop + TrailingStep`, imitando el EA original.
   - Se usan salidas de mercado para emular las llamadas `PositionModify` de MetaTrader cuando el stop o take-profit es tocado.
5. Cuando `UseTimeFilter` está habilitado, las nuevas órdenes se envían solo dentro de ±30 segundos de `StartHour:StartMinute` (hora del servidor), replicando la estrecha ventana de trading del script fuente.

## Gestión de capital

- **FixedVolume**: siempre opera el `TradeVolume` exacto especificado por el usuario.
- **RiskPercent**: calcula el tamaño de la orden de modo que una operación perdedora a la distancia de stop-loss configurada reduzca el capital en `RiskPercent`. Los volúmenes se redondean al `VolumeStep` del instrumento y se limitan entre `MinVolume` / `MaxVolume`.

## Controles de riesgo

- Los niveles iniciales de stop-loss y take-profit se almacenan localmente y se ejecutan mediante órdenes de mercado cuando el precio cruza el objetivo durante la siguiente vela.
- El trailing stop solo se mueve en una dirección y respeta la distancia de paso del EA original, evitando pequeños ajustes constantes.
- Si no se configura stop-loss, el dimensionamiento de posición basado en riesgo vuelve automáticamente al `TradeVolume` fijo.

## Parámetros

| Nombre | Tipo | Por defecto | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | `H4` | Marco temporal usado para construir el rango de ruptura. |
| `BarsCount` | `int` | `15` | Número de velas completadas consideradas al calcular el máximo más alto y mínimo más bajo. |
| `IndentPips` | `decimal` | `3` | Offset (en pips) añadido por encima/debajo del rango antes de enviar órdenes stop. |
| `StopLossPips` | `decimal` | `50` | Distancia protectora del stop en pips. Un valor de `0` deshabilita el stop. |
| `TakeProfitPips` | `decimal` | `50` | Distancia del objetivo de ganancia en pips. Un valor de `0` deshabilita el objetivo. |
| `TrailingStopPips` | `decimal` | `35` | Distancia del trailing stop en pips. Establecer en `0` para deshabilitar el trailing. |
| `TrailingStepPips` | `decimal` | `5` | Ganancia extra mínima requerida antes de ajustar el trailing stop. |
| `MoneyManagement` | `CidomoMoneyManagementMode` | `RiskPercent` | Elige entre tamaño de posición fijo y dimensionamiento basado en riesgo. |
| `RiskPercent` | `decimal` | `1` | Porcentaje de capital arriesgado por operación cuando `MoneyManagement = RiskPercent`. |
| `TradeVolume` | `decimal` | `0.1` | Volumen de orden fijo usado en modo `FixedVolume` o cuando el dimensionamiento basado en riesgo no puede calcularse. |
| `UseTimeFilter` | `bool` | `false` | Habilita el filtro de ventana de tiempo de ±30 segundos. |
| `StartHour` | `int` | `9` | Hora (0-23) del centro de la ventana de trading. |
| `StartMinute` | `int` | `58` | Minuto (0-59) del centro de la ventana de trading. |

## Notas

- Todos los parámetros basados en pips se adaptan automáticamente a cotizaciones de 3 o 5 dígitos multiplicando el `PriceStep` del instrumento por 10, exactamente como la implementación de MetaTrader.
- Dado que StockSharp gestiona los stops del lado del cliente en este port, asegúrate de que la estrategia permanezca conectada para que las salidas de mercado puedan emitirse cuando los niveles de protección sean superados.
