# Estrategia ColorJFatl StDev
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una traducción del asesor experto **ColorJFatl_StDev** de MQL5 a la API de StockSharp. Combina la Media Móvil Jurik (JMA) con bandas de desviación estándar para generar señales de trading.

## Lógica de la estrategia

1. Calcular el JMA sobre los precios de cierre.
2. Calcular la desviación estándar durante un período configurable.
3. Construir dos conjuntos de bandas dinámicas usando los multiplicadores `K1` y `K2`:
   - `upper1 = JMA + K1 * StdDev`
   - `upper2 = JMA + K2 * StdDev`
   - `lower1 = JMA - K1 * StdDev`
   - `lower2 = JMA - K2 * StdDev`
4. Dependiendo del modo de señal seleccionado, la estrategia abre o cierra posiciones:
   - **Point** – se activa cuando el precio cruza las bandas.
   - **Direct** – usa los puntos de giro de la línea JMA.
   - **Without** – desactiva la señal correspondiente.

## Parámetros

| Nombre | Descripción |
|--------|-------------|
| `CandleTimeFrame` | Marco temporal para los datos de velas. |
| `JmaLength` | Período de la Media Móvil Jurik. |
| `JmaPhase` | Fase para el cálculo del JMA. |
| `StdPeriod` | Período para la desviación estándar. |
| `K1` | Primer multiplicador de desviación. |
| `K2` | Segundo multiplicador de desviación. |
| `BuyOpenMode` | Modo para abrir posiciones largas. |
| `SellOpenMode` | Modo para abrir posiciones cortas. |
| `BuyCloseMode` | Modo para cerrar posiciones largas. |
| `SellCloseMode` | Modo para cerrar posiciones cortas. |

## Uso

La estrategia se suscribe a velas del marco temporal especificado, procesa los valores de JMA y desviación estándar, y envía automáticamente órdenes de mercado según los modos definidos.

Esta implementación se centra en la claridad y puede servir como punto de partida para mejoras adicionales o gestión de riesgos personalizada.
