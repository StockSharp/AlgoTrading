# Estrategia XDidi Index Cloud Duplex
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La estrategia XDidi Index Cloud Duplex replica la lógica de señalización dual larga/corta del experto MQL5 original *Exp_XDidi_Index_Cloud_Duplex*. Se evalúan dos configuraciones independientes del índice XDidi en marcos temporales configurables. Cada configuración calcula una relación entre medias móviles rápidas/medias y lentas/medias. Los cruces entre estas relaciones desencadenan entradas de mercado mientras que las divergencias persistentes desencadenan salidas.

## Lógica de trading
1. **Cálculo del indicador**
   - Se calculan tres medias móviles para cada bloque (rápida, media, lenta) en una fuente de precio seleccionada.
   - Las relaciones XDidi se derivan como `fast / medium` y `slow / medium`. La inversión opcional coincide con la opción original `Revers`.
2. **Generación de señales**
   - Bloque largo: cuando la barra anterior tenía `fast > slow` y la barra de señal cierra con `fast <= slow`, se solicita una entrada larga. Si la barra anterior tenía `fast < slow`, se solicita una salida larga.
   - Bloque corto: cuando la barra anterior tenía `fast < slow` y la barra de señal cierra con `fast >= slow`, se solicita una entrada corta. Si la barra anterior tenía `fast > slow`, se solicita una salida corta.
   - Los offsets de barra de señal reproducen las entradas originales `SignalBar`.
3. **Gestión de órdenes**
   - Las entradas se ejecutan con el volumen de la estrategia. Las posiciones opuestas se cierran antes de revertir.
   - Los niveles opcionales de stop-loss y take-profit se aplican via `StartProtection` usando distancias de paso de precio.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `LongCandleType`, `ShortCandleType` | Marcos temporales de velas para cada bloque. |
| `LongFastMethod` / `Medium` / `Slow` & `ShortFastMethod` / `Medium` / `Slow` | Métodos de suavizado de media móvil para curvas rápida, media y lenta. Los suavizadores heredados no soportados vuelven al promediado exponencial. |
| `LongFastLength`, `LongMediumLength`, `LongSlowLength` | Períodos para las medias móviles del bloque largo. |
| `ShortFastLength`, `ShortMediumLength`, `ShortSlowLength` | Períodos para las medias móviles del bloque corto. |
| `LongAppliedPrice`, `ShortAppliedPrice` | Fuente de precio usada para cada bloque (cierre, apertura, típico, Demark, etc.). |
| `EnableLongEntries`, `EnableShortEntries` | Activar/desactivar nuevas posiciones largas/cortas. |
| `EnableLongExits`, `EnableShortExits` | Activar/desactivar salidas automáticas. |
| `LongSignalBar`, `ShortSignalBar` | Desplazamiento histórico (barras atrás) evaluado para cruces. |
| `LongReverse`, `ShortReverse` | Invertir relaciones (refleja el flag `Revers` en MQL). |
| `StopLossPoints`, `TakeProfitPoints` | Distancias de protección expresadas en pasos de precio (establecer en cero para deshabilitar). |
| `Volume` (propiedad base de estrategia) | Define el tamaño de operación predeterminado. |

## Notas de implementación
- Las medias móviles se toman de la biblioteca de indicadores de StockSharp. Los suavizadores avanzados (`JJMA`, `JurX`, `ParMA`, `VIDYA`) usan suavizado exponencial por defecto porque no hay equivalentes directos disponibles.
- Los valores del indicador se procesan solo en velas finalizadas, coincidiendo con el comportamiento original de `IsNewBar`.
- Las colas de señales mantienen solo el número requerido de valores de relación históricos, evitando colecciones pesadas.
- Los stops de protección son opcionales; si ambas distancias son cero, la estrategia aún llama a `StartProtection()` para cumplir con el ciclo de vida del framework.

## Consejos de uso
- Alinee los tipos de velas con la suscripción de datos disponible en su conector.
- Optimice las longitudes de medias móviles y precios aplicados para adaptarse al instrumento negociado.
- Cuando se usan marcos temporales asimétricos (largo/corto), ambas suscripciones se visualizan en áreas de gráfico separadas para mayor claridad.

## Limitaciones en comparación con la versión MQL5
- Los modos de gestión monetaria (`MM`, `MarginMode`) no están replicados; el tamaño de operación sigue la propiedad `Volume` de StockSharp.
- Algunos algoritmos de suavizado exóticos de `SmoothAlgorithms.mqh` se aproximan con medias móviles exponenciales.
- Las órdenes de stop/límite se convierten en niveles de protección genéricos en lugar de parámetros de órdenes individuales.
