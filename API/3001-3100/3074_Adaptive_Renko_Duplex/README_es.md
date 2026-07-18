# Estrategia Adaptive Renko Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia Adaptive Renko Duplex** es un port de StockSharp del asesor experto original `Exp_AdaptiveRenko_Duplex.mq5`. La versión convertida mantiene la idea de ejecutar **dos flujos independientes de Adaptive Renko** – uno dedicado a configuraciones alcistas y otro a bajistas – mientras expone la lógica a través de la API de alto nivel. Cada flujo construye raíles de soporte y resistencia al estilo Renko cuya altura de ladrillo se adapta dinámicamente a la volatilidad reciente. La estrategia reacciona a los cambios de tendencia detectados dentro de estos raíles y puede mantener configuraciones asimétricas para los lados largo y corto.

A diferencia de los sistemas clásicos de trading Renko, que operan con ladrillos sintéticos, el enfoque duplex escucha velas estándar y recalcula continuamente los búferes de Renko adaptativo. Las señales solo se generan en velas completamente terminadas para evitar repintado y para coincidir con el modelo impulsado por eventos de StockSharp.

## Datos de mercado e indicadores
- **Suscripciones de velas** – dos parámetros `DataType` independientes seleccionan las series de velas que alimentan los flujos de Renko largo y corto. Pueden apuntar al mismo marco temporal o a diferentes.
- **Reconstrucción de Adaptive Renko** – cada flujo incorpora la lógica original del indicador. Un tamaño mínimo de ladrillo (expresado en puntos) se compara con `K × volatilidad` y el mayor define la nueva altura del ladrillo. El indicador rastrea envolventes superior/inferior más niveles de tendencia coloreados (soporte en tendencias alcistas, resistencia en bajistas).
- **Fuentes de volatilidad** – elegir entre un indicador `AverageTrueRange` o `StandardDeviation`. Ambos operan en la serie de velas usada por su flujo respectivo y aceptan longitudes de retroceso personalizadas.

## Lógica de trading
1. **Detección del lado largo**
   - El flujo largo construye ladrillos adaptativos usando los parámetros configurados.
   - Cuando la línea de tendencia alcista (`RenkoTrend.Up`) aparece en la barra retrasada definida por `LongSignalBarOffset`, la estrategia emite una orden de compra de mercado. El tamaño de la orden es `Volume + |Position|`, permitiendo reversiones inmediatas de corto a largo.
   - Si se detecta una línea de tendencia bajista después del retraso configurado y `LongExitsEnabled` es verdadero, toda la exposición larga se cierra.
2. **Detección del lado corto**
   - El flujo corto refleja la lógica: una señal `RenkoTrend.Down` produce una venta de mercado, mientras que `RenkoTrend.Up` en la barra retrasada sale de cortos cuando `ShortExitsEnabled` está habilitado.
3. **Retraso de señal** – ambos lados respetan sus parámetros `SignalBarOffset`, reproduciendo el desplazamiento de una barra usado por el experto de MetaTrader. Establecer el desplazamiento en cero reacciona en el vela terminado más reciente.
4. **Dimensionamiento de posición** – la versión de StockSharp depende de la propiedad `Volume` de la estrategia. Siempre configurarla antes de iniciar la estrategia.

## Gestión de riesgo
- **Stop-loss / take-profit** – las distancias se especifican en **puntos** y se multiplican por el `PriceStep` del instrumento para producir precios absolutos. Los stops se verifican cuando cierra un vela suscrito. Debido a que StockSharp no crea automáticamente órdenes protectoras del lado del servidor, las salidas se manejan mediante órdenes de mercado.
- **Seguimiento de estado** – la estrategia almacena el precio al que se ejecutó la última entrada larga o corta (basado en el cierre del vela) para poder evaluar la distancia al stop o objetivo.
- **Anulaciones manuales** – los módulos estándar de `Stop` o `Protective` pueden adjuntarse encima llamando a `StartProtection()` externamente si se requiere gestión de riesgo a nivel de cuenta.

## Parámetros
| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `LongCandleType` | Velas de 4 horas | Serie de velas usada para calcular señales largas. |
| `ShortCandleType` | Velas de 4 horas | Serie de velas usada para calcular señales cortas. |
| `LongVolatilityMode` | ATR | Fuente de volatilidad (`AverageTrueRange` o `StandardDeviation`) para ladrillos largos. |
| `ShortVolatilityMode` | ATR | Fuente de volatilidad para ladrillos cortos. |
| `LongVolatilityPeriod` | 10 | Período de retroceso para el indicador de volatilidad largo. |
| `ShortVolatilityPeriod` | 10 | Período de retroceso para el indicador de volatilidad corto. |
| `LongSensitivity` | 1.0 | Multiplicador aplicado al valor de volatilidad antes de construir ladrillos largos. |
| `ShortSensitivity` | 1.0 | Multiplicador aplicado al valor de volatilidad antes de construir ladrillos cortos. |
| `LongPriceMode` | Close | Entrada de precio (`HighLow` o `Close`) usada para actualizar los raíles de Renko largo. |
| `ShortPriceMode` | Close | Entrada de precio usada para actualizar los raíles de Renko corto. |
| `LongMinimumBrickPoints` | 2 | Altura mínima de ladrillo para el flujo largo, medida en puntos. |
| `ShortMinimumBrickPoints` | 2 | Altura mínima de ladrillo para el flujo corto. |
| `LongSignalBarOffset` | 1 | Retraso (en barras) antes de confirmar una señal larga. |
| `ShortSignalBarOffset` | 1 | Retraso (en barras) antes de confirmar una señal corta. |
| `LongEntriesEnabled` | true | Activar para permitir o bloquear entradas largas. |
| `LongExitsEnabled` | true | Activar para permitir o bloquear salidas largas impulsadas por Renko. |
| `ShortEntriesEnabled` | true | Activar para permitir o bloquear entradas cortas. |
| `ShortExitsEnabled` | true | Activar para permitir o bloquear salidas cortas impulsadas por Renko. |
| `LongStopLossPoints` | 1000 | Distancia de stop-loss para posiciones largas (puntos × `PriceStep`). |
| `LongTakeProfitPoints` | 2000 | Distancia de take-profit para posiciones largas. |
| `ShortStopLossPoints` | 1000 | Distancia de stop-loss para posiciones cortas. |
| `ShortTakeProfitPoints` | 2000 | Distancia de take-profit para posiciones cortas. |

> **Conversión de puntos** – la versión MQL usó la definición de "punto" del broker. En StockSharp cada distancia se multiplica por `Security.PriceStep` (o `Security.MinStep` como respaldo) para convertir puntos en incrementos de precio absolutos. Ajustar los valores predeterminados para el tamaño de tick de su instrumento.

## Guías de uso
1. **Configurar el entorno** – asignar `Security`, `Portfolio` y `Volume` antes de iniciar la estrategia. Asegurarse de que la fuente de datos pueda entregar todos los marcos temporales de velas configurados.
2. **Personalizar ambos flujos** – puede mantener la configuración simétrica predeterminada o asignar diferentes marcos temporales/modos de volatilidad a los lados largo y corto para comportamiento asimétrico.
3. **Monitorear registros** – la estrategia emite mensajes `LogInfo` en cada entrada y salida, indicando el nivel de Renko que desencadenó la acción. Usar estos registros para validar que las señales coincidan con las expectativas.
4. **Combinar con módulos externos** – filtros adicionales (control de sesión, protección de capital, etc.) pueden adjuntarse a través de las APIs de alto nivel de StockSharp porque la estrategia expone las señales en la clase `Strategy` principal.
5. **Consideraciones de backtesting** – al probar con datos históricos, preferir constructores de velas que puedan reconstruir los marcos temporales requeridos para que el Renko adaptativo permanezca consistente.

## Diferencias respecto al asesor experto original
- Las características específicas de MetaTrader (números mágicos, modos de gestión de dinero, manejo de desviaciones, notificaciones push) se omiten intencionalmente. El dimensionamiento de posición depende únicamente de la propiedad `Volume` de StockSharp.
- El EA original colocaba órdenes de stop-loss y take-profit del lado del servidor. La versión convertida verifica las distancias configuradas en cada vela terminada y cierra mediante órdenes de mercado.
- Las señales se evalúan estrictamente en velas completadas para evitar recálculos de barra parcial. Esto replica la verificación `IsNewBar` usada en la implementación MQL.
- La reconstrucción del Renko adaptativo sigue el algoritmo publicado pero se implementa en C# sin crear objetos indicadores adicionales, lo que mantiene la ruta de actualización eficiente mientras respeta las convenciones de la API de alto nivel de StockSharp.

## Mejoras recomendadas
- Combinar el flujo duplex con filtros de régimen de nivel superior (horarios de sesión, filtros de volatilidad) para evitar operar en condiciones ilíquidas.
- Adjuntar módulos de stop de seguimiento o protecciones basadas en capital mediante `StartProtection()` para salvaguardias a nivel de cuenta.
- Registrar o graficar los raíles de soporte/resistencia generados para validar visualmente la estrategia durante la revisión discrecional.
