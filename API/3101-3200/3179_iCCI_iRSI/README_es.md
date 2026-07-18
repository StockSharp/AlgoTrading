# Estrategia de iCCI iRSI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de iCCI iRSI** es una conversión directa del asesor experto de MetaTrader 5 `iCCI iRSI.mq5`. El sistema original combina el Índice de Canal de Materias Primas (CCI) y el Índice de Fuerza Relativa (RSI) para detectar zonas de agotamiento. Cuando ambos osciladores coinciden en un estado de sobreventa o sobrecompra, el asesor abre una posición, adjunta órdenes protectoras y opcionalmente sigue el stop mientras la operación entra en beneficio. Este port de StockSharp refleja ese comportamiento con APIs de alto nivel, incluyendo entradas basadas en pips, cierre automático de posiciones opuestas y un modo de señal reversible.

## Lógica de trading
1. Suscribirse al tipo de vela configurado y calcular un `CommodityChannelIndex` con período `CciPeriod` y un `RelativeStrengthIndex` con período `RsiPeriod`.
2. Evaluar solo velas completadas. El ruido intrabarra se ignora igual que la implementación MQL que espera una nueva barra.
3. Cuando ambos indicadores caen por debajo de sus respectivos umbrales inferiores (`CciLowerLevel` y `RsiLowerLevel`), la estrategia abre o revierte a una posición larga. Cuando ambos indicadores suben por encima de los umbrales superiores (`CciUpperLevel` y `RsiUpperLevel`), se activa una configuración corta. Habilitar `ReverseSignals` intercambia las direcciones.
4. Antes de enviar una nueva orden, la exposición opuesta actual se cierra para que la posición neta siempre coincida con la señal activa.
5. Después de la entrada, la estrategia monitoriza el precio de cierre de las velas siguientes. Los niveles de take-profit y stop-loss expresados en pips se convierten a unidades de precio usando el `PriceStep` del instrumento. Para símbolos forex de 3 o 5 dígitos, un ajuste adicional de ×10 reproduce la definición de pip de MetaTrader.
6. Si `TrailingStopPips` es positivo, el stop-loss se avanza hacia el mercado siempre que el precio se mueva más de `TrailingStopPips + TrailingStepPips` en la dirección favorable. Las actualizaciones respetan el paso configurado para evitar modificaciones rápidas del stop.

## Gestión de riesgo y operaciones
- **Take-profit / Stop-loss** – distancias opcionales en pips que se convierten en niveles de precio absolutos inmediatamente después de un llenado. Cuando alguno de los niveles es alcanzado en el cierre de una vela, la posición se liquida a mercado.
- **Trailing stop** – replica la lógica de trailing del EA. Los beneficios deben superar la distancia de trailing más el paso de trailing antes de que el stop se ajuste.
- **Volumen** – un parámetro `TradeVolume` fijo reemplaza el selector original de lote o riesgo (`ENUM_LOT_OR_RISK`). Use optimización para descubrir volúmenes adecuados si se requieren variantes de gestión monetaria.
- **Higiene de posición** – cuando aparece una nueva señal, la estrategia aplana cualquier tenencia opuesta antes de abrir la nueva operación, igual que el EA realiza `ClosePositions`.

## Parámetros
- **Candle Type** – serie de datos de velas procesada por los indicadores (predeterminado: velas de 1 hora).
- **CciPeriod** – longitud de promediación CCI (predeterminado: 14).
- **CciUpperLevel / CciLowerLevel** – umbrales de sobrecompra y sobreventa CCI (predeterminados: +80 / −80).
- **RsiPeriod** – longitud de promediación RSI (predeterminado: 42).
- **RsiUpperLevel / RsiLowerLevel** – niveles de disparo RSI (predeterminados: 60 / 30).
- **ReverseSignals** – invierte la interpretación de las señales del oscilador (predeterminado: `false`).
- **TradeVolume** – tamaño de la orden de mercado. Establecer para que coincida con la entrada de lote MT5 (predeterminado: 0.1).
- **StopLossPips / TakeProfitPips** – distancias protectoras en pips (predeterminados: 0 y 140). Establecer en cero para deshabilitar.
- **TrailingStopPips / TrailingStepPips** – distancia del trailing stop y paso mínimo (predeterminados: 5 / 5). Una distancia de trailing cero deshabilita el trailing incluso si se proporciona un paso.

## Notas de implementación
- Los indicadores de StockSharp (`CommodityChannelIndex`, `RelativeStrengthIndex`) entregan valores decimales listos para usar a través de la API `Bind`, por lo que no se requiere lógica manual `CopyBuffer`.
- Toda la gestión de operaciones tiene lugar en velas completadas. Esto coincide con la guardia `PrevBars` del EA y previene múltiples entradas dentro de la misma barra.
- La conversión de pips respeta las cotizaciones pip fraccionales multiplicando el `PriceStep` por 10 para instrumentos con 3 o 5 decimales – un análogo directo de la lógica `digits_adjust` de MQL.
- Los objetivos protectores se simulan mediante salidas de mercado porque las estrategias de StockSharp operan dentro de un entorno en sandbox donde las modificaciones de órdenes síncronas no están disponibles.
- Áreas de gráfico adicionales dibujan las líneas CCI y RSI para validación visual de las zonas de entrada.

## Diferencias respecto al Asesor Experto original
- El módulo de MetaTrader `MoneyFixedMargin` no está portado. El dimensionamiento de posición es ahora un parámetro de volumen fijo simple.
- Las verificaciones específicas del broker como `FreezeStopsLevels` no están disponibles en StockSharp. El trailing stop solo observa la distancia y los requisitos de paso del precio.
- Las cadenas de logging y alerta se han eliminado a favor de una salida de estrategia limpia. El sistema de logging de StockSharp puede adjuntarse externamente si es necesario.
- La gestión de operaciones funciona en los cierres de las velas. La versión MT5 podría reaccionar dentro de la barra cuando se toca el stop o take-profit, pero la aproximación al final de la barra mantiene la lógica determinista para los backtests.

## Consejos de uso
1. Comenzar con el marco temporal de 1 hora predeterminado para reflejar la plantilla original. Los marcos más cortos pueden introducir más señales pero también más trampas.
2. Optimizar `CciUpperLevel`, `CciLowerLevel`, `RsiUpperLevel` y `RsiLowerLevel` juntos – el EA depende de la concordancia entre ambos osciladores, por lo que los umbrales equilibrados son esenciales.
3. Al operar con pares forex, verificar que los metadatos del instrumento exponen `PriceStep` y `Decimals` para que las distancias pip se conviertan correctamente.
4. Deshabilitar `ReverseSignals` para el comportamiento clásico de reversión de tendencia. Habilitarlo para operar rupturas fuera de zonas de sobrecompra/sobreventa.
5. Combinar con módulos de riesgo de StockSharp (stop de patrimonio, protección de drawdown) si se requieren controles a nivel de cartera – reemplazan el helper `m_money` de MT5.

Esta documentación proporciona todo el contexto necesario para desplegar, personalizar y extender la estrategia iCCI iRSI dentro del entorno de StockSharp.
