# Estrategia de Divergencia RSI & CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de Divergencia RSI & CCI** es una conversión del asesor experto de MetaTrader `RSI&CCI_DIVERGENCE.mq4` (MQL ID 22266). El sistema busca divergencias bajistas o alcistas entre máximos de precio y dos osciladores (Commodity Channel Index y Relative Strength Index), los filtra con un filtro de tendencia de media móvil lineal ponderada, valida la señal con alineación MACD en tres marcos temporales diferentes y confirma la fuerza del momentum usando un oscilador de momentum en un marco temporal superior. Se pueden aplicar objetivos opcionales absolutos de stop-loss y take-profit para gestionar las posiciones abiertas.

La implementación de StockSharp se centra en la API de alto nivel. Los indicadores se enlazan directamente a las suscripciones de velas y todos los cálculos son impulsados por actualizaciones de velas en streaming sin recuperación manual de valores del indicador.

## Lógica de trading
1. **Filtro de tendencia**
   - Las medias móviles lineales ponderadas (LWMA) rápidas y lentas en el marco temporal primario definen la dirección prevaleciente.
   - El contexto alcista requiere que la LWMA rápida esté por encima de la LWMA lenta; el contexto bajista requiere lo contrario.

2. **Detección de divergencia**
   - La última vela cerrada se compara con hasta `CandlesToRetrace` velas anteriores.
   - Una señal alcista ocurre si CCI o RSI hace un mínimo más alto mientras la vela anterior correspondiente muestra un máximo más alto que el último máximo (divergencia alcista).
   - Una señal bajista ocurre si CCI o RSI hace un máximo más bajo mientras la vela anterior correspondiente muestra un máximo más bajo que el último máximo (divergencia bajista).

3. **Confirmación MACD**
   - El MACD (12, 26, 9 por defecto) se evalúa en los marcos temporales primario, superior y macro.
   - Las operaciones largas requieren que el MACD esté por encima de la línea de señal en todos los marcos temporales.
   - Las operaciones cortas requieren que el MACD esté por debajo de la línea de señal en todos los marcos temporales.

4. **Confirmación de momentum**
   - Un oscilador de momentum (longitud 14 por defecto) se muestrea en un marco temporal superior (por defecto 1 hora).
   - La desviación absoluta de las lecturas de momentum recientes del nivel neutral 100 debe superar los umbrales de compra/venta configurados para aprobar la operación.

5. **Guardia de estructura de precio**
   - La estrategia verifica máximos/mínimos recientes para imitar las restricciones del EA original (`Low[2] < High[1]` para largos y `Low[1] < High[2]` para cortos).

6. **Ejecución de órdenes**
   - Cuando todos los filtros se alinean, la estrategia entra usando `BuyMarket` o `SellMarket` con volumen igual al volumen base de la estrategia más el valor absoluto de la posición actual, permitiendo reversión inmediata.

7. **Gestión de riesgo**
   - Las distancias absolutas opcionales de stop-loss y take-profit se evalúan en cada vela finalizada.
   - Si se configuran, la estrategia envía una orden de mercado dimensionada para liquidar la posición cuando se toca el stop o el objetivo.

## Parámetros
| Nombre | Por defecto | Descripción |
| --- | --- | --- |
| `FastMaLength` | 6 | Período para el filtro de tendencia LWMA rápida. |
| `SlowMaLength` | 85 | Período para el filtro de tendencia LWMA lenta. |
| `CciLength` | 14 | Período de retrospectiva para el Commodity Channel Index. |
| `RsiLength` | 14 | Período de retrospectiva para el Relative Strength Index. |
| `CandlesToRetrace` | 10 | Número de velas completadas usadas para detectar divergencias. |
| `MacdFastPeriod` | 12 | Período de media móvil rápida en el cálculo MACD. |
| `MacdSlowPeriod` | 26 | Período de media móvil lenta en el cálculo MACD. |
| `MacdSignalPeriod` | 9 | Período de línea de señal para MACD. |
| `MomentumLength` | 14 | Longitud del oscilador de momentum en marco temporal superior. |
| `MomentumBuyThreshold` | 0.3 | Desviación absoluta mínima de 100 para confirmación de momentum alcista. |
| `MomentumSellThreshold` | 0.3 | Desviación absoluta mínima de 100 para confirmación de momentum bajista. |
| `StopLoss` | 0 | Distancia absoluta de precio para un stop-loss opcional (0 deshabilita el stop). |
| `TakeProfit` | 0 | Distancia absoluta de precio para un take-profit opcional (0 deshabilita el objetivo). |
| `CandleType` | Marco temporal de 15 minutos | Tipo de vela primaria para análisis de divergencia y tendencia. |
| `MomentumCandleType` | Marco temporal de 1 hora | Tipo de vela usada para la confirmación de momentum. |
| `HigherMacdCandleType` | Marco temporal de 1 hora | Marco temporal secundario para confirmación MACD. |
| `MacroMacdCandleType` | Marco temporal de 30 días | Marco temporal macro para confirmación MACD (ajustar para coincidir con la disponibilidad de datos del instrumento). |

## Notas de uso
- Asegúrese de que todos los marcos temporales referenciados estén disponibles desde el proveedor de datos; de lo contrario, ajuste los parámetros de tipo de vela en consecuencia.
- Los valores predeterminados de stop-loss y take-profit están deshabilitados para reflejar el comportamiento original del EA donde el riesgo se gestionaba mediante stops de trailing y equidad. Establezca valores decimales positivos para habilitar stops duros.
- Debido a que la confirmación de momentum compara valores con la línea base de 100, asume que el indicador `Momentum` de StockSharp usa la definición clásica (`100 * Close / Close[N]`). Si se prefiere una normalización diferente, ajuste los umbrales para coincidir con la volatilidad del instrumento.
- La estrategia envía órdenes de mercado tanto para entradas como salidas, reflejando la lógica de ejecución inmediata del asesor experto fuente.

## Notas de conversión
- La conversión usa el enlace de indicadores de alto nivel de StockSharp. No se requieren llamadas manuales a `GetValue`; los valores del indicador son proporcionados por los callbacks de enlace.
- La gestión de stop basada en equidad, la lógica de trailing y las características de correo electrónico/notificación de la fuente MQL no se portan. En cambio, el enfoque se coloca en la generación de señales primaria y el manejo básico de stop/objetivo.
- La detección de divergencia se implementa usando listas ligeras para mantener el historial reciente de precio e indicadores necesario para el reconocimiento de patrones.
