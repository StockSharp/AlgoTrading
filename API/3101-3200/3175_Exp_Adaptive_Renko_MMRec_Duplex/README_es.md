# Estrategia de Exp Adaptive Renko MMRec Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el asesor experto de MetaTrader 5 **Exp_AdaptiveRenko_MMRec_Duplex.mq5** a la API de alto nivel de StockSharp. Dos flujos independientes de Adaptive Renko —uno configurado para oportunidades largas y otro para cortas— observan cómo los canales de ladrillos personalizados cambian entre soporte y resistencia. Cuando el canal largo reporta nuevo soporte mientras el canal corto pierde resistencia (o viceversa), la estrategia abre la posición de mercado correspondiente. La versión C# conserva el bloque original de gestión monetaria "MM Recounter" que reduce el tamaño de la operación tras una serie configurable de pérdidas y lo restaura una vez que la racha termina.

## Flujo de trabajo principal

1. **Suscripciones de datos** – cada lado se suscribe a su propio tipo de vela (marco temporal) y vincula un indicador de volatilidad (ATR o Desviación Estándar) a través de `SubscribeCandles().BindEx(...)`. El indicador impulsa la altura adaptativa del ladrillo.
2. **Procesamiento de Adaptive Renko** – el helper `AdaptiveRenkoProcessor` reconstruye la lógica del indicador MQL, devolviendo una instantánea con la tendencia más reciente y los niveles de soporte y resistencia. Las señales se evalúan solo en velas completadas.
3. **Lógica de entrada** – cuando la instantánea de Renko largo indica una tendencia alcista (el soporte aparece en la barra de señal), la estrategia abre una posición larga. Las entradas cortas requieren una tendencia bajista del flujo corto.
4. **Lógica de salida** – los eventos de Renko opuestos cierran una posición activa. Verificaciones adicionales aplican distancias de stop-loss y take-profit expresadas en pasos de precio.
5. **Gestión monetaria MMRec** – cada dirección mantiene una cola de valores de PnL realizados recientes. Si el número de pérdidas dentro de la ventana configurada alcanza el disparador de pérdidas, la siguiente orden usa el valor de gestión monetaria reducido (`LongSmallMoneyManagement` / `ShortSmallMoneyManagement`). De lo contrario, se usa el valor normal (`LongMoneyManagement` / `ShortMoneyManagement`). El enum `MarginModeOption` reproduce los modos de dimensionamiento MQL (lote, participación de saldo, participación basada en pérdidas, etc.).
6. **Registro de operaciones** – cada salida llama a `RegisterTradeResult` para alimentar las colas MMRec. El recorte de la cola replica las funciones originales `BuyTradeMMRecounterS` y `SellTradeMMRecounterS` sin escanear el historial del terminal.

## Grupos de parámetros

| Grupo | Parámetros clave | Descripción |
| --- | --- | --- |
| Lado largo | `LongCandleType`, `LongVolatilityMode`, `LongVolatilityPeriod`, `LongSensitivity`, `LongPriceMode`, `LongMinimumBrickPoints`, `LongSignalBarOffset` | Controlan el flujo de Adaptive Renko que produce entradas largas. |
| Lado corto | `ShortCandleType`, `ShortVolatilityMode`, `ShortVolatilityPeriod`, `ShortSensitivity`, `ShortPriceMode`, `ShortMinimumBrickPoints`, `ShortSignalBarOffset` | Replican la configuración para el módulo corto. |
| MMRec | `LongTotalTrigger`, `LongLossTrigger`, `LongSmallMoneyManagement`, `LongMoneyManagement`, `LongMarginMode`, `ShortTotalTrigger`, `ShortLossTrigger`, `ShortSmallMoneyManagement`, `ShortMoneyManagement`, `ShortMarginMode` | Replican el bloque de recuperación de gestión monetaria. Los parámetros *TotalTrigger* definen el tamaño de la ventana deslizante, *LossTrigger* el recuento de pérdidas que activa el volumen reducido. |
| Riesgo | `LongStopLossPoints`, `LongTakeProfitPoints`, `ShortStopLossPoints`, `ShortTakeProfitPoints`, `LongDeviationSteps`, `ShortDeviationSteps` | Expresan niveles protectores y deslizamiento informativo en pasos de precio. |

## Notas de comportamiento

- La estrategia funciona en el modelo de cuenta de compensación: antes de abrir una operación larga cierra cualquier corta pendiente y viceversa.
- Los tamaños de posición se calculan a través de `CalculateVolume`. El helper admite todos los modos de margen originales, incluido el dimensionamiento basado en pérdidas que depende de la distancia de stop-loss configurada.
- Todo el procesamiento de indicadores ocurre solo en velas completadas, respetando el EA fuente.
- Los registros incluyen el multiplicador de gestión monetaria y el deslizamiento esperado (en pasos) para rastreabilidad.

## Archivos

- `CS/ExpAdaptiveRenkoMmrecDuplexStrategy.cs` – implementación de la estrategia con el procesador de Adaptive Renko y el módulo MMRec.
- `README.md` – documentación en inglés (este archivo).
- `README_ru.md` – documentación en ruso.
- `README_zh.md` – documentación en chino.
