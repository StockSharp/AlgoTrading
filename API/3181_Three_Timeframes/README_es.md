# Estrategia de Three Timeframes
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Three Timeframes** replica el experto de MetaTrader `Three timeframes.mq5` usando la API de alto nivel de StockSharp. El sistema combina filtros de momentum y tendencia tomados de diferentes marcos temporales:

- **MACD (M5)** detecta reversiones de momentum recientes en el marco temporal de trading.
- **Alligator (H4)** verifica que la estructura del marco temporal superior esté alineada con la dirección de operación prevista.
- **RSI (H1)** confirma que el momentum en el marco temporal intermedio apoya la ruptura.
- **Filtrado de sesión** opcional bloquea las operaciones fuera de las horas de trabajo configuradas.

La estrategia usa gestión de riesgo basada en pips. Los niveles iniciales de stop-loss y take-profit se adjuntan a cada nueva posición. Cuando el precio avanza, un trailing stop opcional ajusta el stop protector después de que el mercado cubra tanto la distancia de trailing como el paso de trailing.

## Lógica de señales
1. Los precios se procesan en tres suscripciones diferentes: velas de trading, velas de marco temporal superior para el Alligator y velas intermedias para el RSI.
2. Una configuración larga requiere:
   - La línea principal del MACD cruzando **por debajo** de la línea de señal en la barra anterior mientras que la barra anterior a esa estaba por encima de la línea de señal, reproduciendo la regla de MetaTrader "azul cruza rojo hacia abajo".
   - RSI en el feed H1 por encima de 50.
   - Alligator jaw > teeth > lips en la vela H4 completada anterior, indicando una estructura alcista.
3. Una configuración corta replica las reglas: la línea principal del MACD cruza por encima de la línea de señal, el RSI está por debajo de 50 y lips > teeth > jaw en el Alligator para confirmar una estructura bajista.
4. Si existe una posición opuesta, la estrategia la cierra enviando una orden de mercado por el tamaño neto, igual que el EA original antes de abrir una nueva operación.
5. Después de la entrada, la estrategia aplica las distancias iniciales de stop-loss/take-profit y continúa siguiendo el stop una vez que el precio se mueve `TrailingStopPips + TrailingStepPips` desde la entrada.

El filtro de sesión de trading refleja la implementación de MetaTrader. Cuando la hora de inicio es menor que la hora de fin, el trading solo se permite dentro del intervalo. Cuando la hora de inicio es mayor que la hora de fin, la ventana activa cruza la medianoche.

## Gestión de riesgo
- **Stop Loss / Take Profit** – las distancias se expresan en pips. La estrategia los convierte a unidades de precio usando el paso de precio del símbolo y ajusta para cotizaciones FX de 3 o 5 dígitos.
- **Trailing Stop** – se activa una vez que la operación cubre tanto la distancia de trailing como el paso de trailing. El stop se mueve entonces a `price - trailing distance` para largos y `price + trailing distance` para cortos.
- **Volumen de trading** – especifica el tamaño base del lote para nuevas órdenes de mercado. La exposición opuesta se aplana automáticamente antes de revertir.

## Diferencias respecto a la versión de MetaTrader
- El modelo de órdenes asíncronas de StockSharp elimina la necesidad de flags explícitos de seguimiento de transacciones (`m_waiting_transaction`). Las órdenes se ejecutan usando `BuyMarket`/`SellMarket`, que ya esperan confirmaciones internamente.
- Las configuraciones de deslizamiento, política de llenado y modo de margen de la versión MQL están abstraídas por StockSharp. Estos controles específicos de plataforma no son necesarios para la implementación .NET.
- El indicador Alligator se reconstruye desde medias móviles suavizadas preservando los períodos y desplazamientos originales. Los valores del indicador se almacenan en buffers deslizantes para reproducir el comportamiento de desplazamiento del Alligator integrado de MetaTrader.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `TradeVolume` | Tamaño de la orden de mercado en lotes/contratos. | `1` |
| `StopLossPips` | Distancia inicial de stop-loss en pips. | `50` |
| `TakeProfitPips` | Distancia inicial de take-profit en pips. | `140` |
| `TrailingStopPips` | Distancia del trailing stop en pips. | `5` |
| `TrailingStepPips` | Movimiento de pips adicional requerido antes de mover el trailing stop. | `5` |
| `MacdFastPeriod` | Longitud de EMA rápida para MACD. | `13` |
| `MacdSlowPeriod` | Longitud de EMA lenta para MACD. | `26` |
| `MacdSignalPeriod` | Período de suavizado de señal para MACD. | `10` |
| `JawPeriod`, `TeethPeriod`, `LipsPeriod` | Períodos SMMA del Alligator para jaw/teeth/lips. | `13`, `8`, `5` |
| `JawShift`, `TeethShift`, `LipsShift` | Desplazamientos hacia adelante para las líneas del Alligator. | `8`, `5`, `3` |
| `RsiPeriod` | Longitud de promediación RSI en el marco temporal intermedio. | `14` |
| `CandleType` | Marco temporal de trading (predeterminado velas de 5 minutos). | `M5` |
| `AlligatorCandleType` | Marco temporal superior para el cálculo del Alligator (predeterminado velas de 4 horas). | `H4` |
| `RsiCandleType` | Marco temporal intermedio para confirmación RSI (predeterminado velas de 1 hora). | `H1` |
| `UseTimeFilter` | Habilita el filtro de sesión. | `true` |
| `StartHour` | Hora de inicio de sesión (inclusiva). | `10` |
| `EndHour` | Hora de fin de sesión (exclusiva). | `15` |

## Notas de uso
- Asegúrese de que el instrumento seleccionado proporcione los tres flujos de velas configurados (M5, H1, H4 por defecto). StockSharp solicitará automáticamente todas las suscripciones necesarias a través de `GetWorkingSecurities()`.
- La conversión de pips depende de `Security.PriceStep`. Los instrumentos con tamaños de tick inusuales pueden necesitar ajuste manual de los parámetros de stop/take.
- Los trailing stops requieren que tanto `TrailingStopPips` como `TrailingStepPips` sean mayores que cero. Establecer cualquiera de los parámetros en cero deshabilita la lógica de trailing, de forma consistente con el experto MQL.
