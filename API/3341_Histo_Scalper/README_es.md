# Estrategia de revendedor histórico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
**Histo Scalper Strategy** es una adaptación de C# del asesor experto MetaTrader *HistoScalperEA v1.0*. El algoritmo fusiona ocho indicadores de estilo histograma (ADX, ATR, Bollinger bandas, potencia de toros/osos, CCI, MACD, RSI y Stochastic) y requiere un acuerdo unánime de todos los filtros habilitados antes de abrir una operación. Un segundo requisito es que al menos un filtro informe la dirección opuesta en la barra anterior, lo que evita que la estrategia entre durante mercados planos e imita la lógica de confirmación original de "dos barras".

## Generación de señal
1. Filtro **ADX**: comprueba si +DI es mayor que −DI. Opcionalmente, invierta la decisión.
2. Filtro **ATR**: compara el ATR actual con una línea base SMA y mide la desviación porcentual. Las operaciones largas requieren una desviación positiva por encima de `AtrPositiveThreshold`; Las operaciones cortas requieren una desviación negativa por debajo de `AtrNegativeThreshold`.
3. **Bollinger ruptura**: espera que el precio de cierre rompa la banda superior/inferior.
4. **Poder de Bulls/Bears**: utiliza Bulls Power para entradas largas y magnitud de Bears Power para entradas cortas.
5. **CCI**: se activa cuando el valor de CCI cruza los niveles de sobreventa/sobrecompra configurados.
6. **MACD histograma**: mide la distancia entre MACD y su línea de señal.
7. **RSI** – utiliza zonas clásicas de sobreventa/sobrecompra.
8. **Stochastic**: lee la línea %K y la compara con los límites configurados.

Si algún filtro habilitado produce un valor neutral, la estrategia cancela el procesamiento de la vela actual. El estado histórico de cada filtro se almacena para aplicar la regla de "barra anterior opuesta".

## Gestión del riesgo
* Las entradas al mercado utilizan el parámetro `TradeVolume`.
* La piramidización opcional se suma a las posiciones abiertas; de lo contrario, la estrategia sólo cambia de dirección cuando cambia la señal.
* Los niveles de toma de ganancias y límite de pérdidas se expresan en incrementos del precio del instrumento y se aplican inmediatamente después del envío de la orden a través de `SetTakeProfit` y `SetStopLoss`.
* Un filtro de sesión (`UseTimeFilter`, `SessionStart`, `SessionEnd`) puede deshabilitar el comercio fuera del horario configurado.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `TradeVolume` | Volumen base para nuevas operaciones.
| `AllowPyramiding` | Permite acumular operaciones adicionales mientras ya está posicionada.
| `CloseOnOppositeSignal` | Cierra posiciones existentes cuando la señal agregada cambia.
| `UseTimeFilter`, `SessionStart`, `SessionEnd` | Restringe el comercio a una ventana diaria personalizada.
| `UseTakeProfit`, `TakeProfitPoints` | Habilita y configura la toma de ganancias en pasos de precio.
| `UseStopLoss`, `StopLossPoints` | Habilita y configura stop loss en pasos de precio.
| `UseIndicator1` … `UseIndicator8` | Habilite filtros individuales.
| `ModeIndicatorX` | Cambie entre lógica directa e invertida para cada filtro.
| Configuraciones específicas del indicador | Períodos, umbrales y niveles que replican los aportes originales del asesor experto.

## Diferencias con el experto MQL
* Se omiten intencionadamente la gestión de pérdidas y ganancias de la cesta, las alertas sonoras y la gestión de pedidos de la red.
* No se incluye la automatización de riesgos (tamaño de lote de automóviles, equilibrio y lógica de seguimiento); utilice los parámetros de riesgo anteriores en su lugar.
* Los controles de diferenciales y las protecciones específicas de los corredores no se transfieren.

## Notas de uso
1. Establezca `Security` y `Portfolio` antes de comenzar la estrategia.
2. Ajuste el tipo de vela (`CandleType`) para que coincida con el período de tiempo deseado.
3. Configure los umbrales de los indicadores para que se ajusten a la volatilidad del instrumento objetivo.
4. Habilite o deshabilite los filtros individualmente para simplificar la optimización.
5. Utilice `AllowPyramiding` y `CloseOnOppositeSignal` para controlar la exposición durante los mercados rápidos.
