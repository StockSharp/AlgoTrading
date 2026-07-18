# Estrategia de Operador Técnico
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Technical Trader reimplementa el asesor experto de MetaTrader de `MQL/22304/Technical_trader.mq5` combinando dos medias móviles simples con un detector adaptativo de clústeres de liquidez. La estrategia busca niveles de precio negociados repetidamente cerca del bid/ask actual y solo abre operaciones cuando esos clústeres se alinean con la dirección del cruce de las SMAs rápida/lenta. El riesgo se controla mediante offsets de stop-loss y take-profit basados en pasos de precio que reflejan la configuración original de MQL.

## Visión general
- **Plataforma:** API de estrategia de alto nivel de StockSharp.
- **Datos de mercado:** Velas definidas por marco temporal más instantáneas del libro de órdenes para obtener los precios bid/ask actuales.
- **Estilo:** Seguimiento de ruptura direccional siguiendo clústeres de liquidez cercanos.
- **Mapeo de fuente:** El cruce de SMA, el muestreo histórico de cierres, la tolerancia de clustering y el dimensionamiento de órdenes fueron portados del experto MQL.

## Lógica de trading
1. Suscribirse a velas del marco temporal configurado y calcular dos SMAs (`FastMaPeriod` y `SlowMaPeriod`).
2. Mantener una ventana deslizante (`HistoryDepth`) de los precios de cierre más recientes y redondearlos a tres decimales, emulando el comportamiento original de `NormalizeDouble`.
3. Construir un histograma de ocurrencias de precios y clasificar niveles cuya frecuencia supera `ResistanceThreshold`.
4. Rastrear el bid y ask más recientes usando el libro de órdenes; recurrir al cierre de la vela si no hay cotizaciones disponibles.
5. Condiciones de entrada larga:
   - La SMA rápida está por encima de la SMA lenta.
   - Un clúster de precios calificado se encuentra justo debajo del ask actual (la `LevelTolerance` define la distancia permitida).
   - Si la estrategia está plana o corta, compra suficiente volumen para cubrir el corto y establecer la posición larga de volumen base.
6. Las condiciones de entrada corta reflejan la lógica larga pero usan clústeres justo encima del bid y requieren que la SMA rápida esté por debajo de la SMA lenta.
7. Al entrar en una posición, calcula los niveles de stop-loss y take-profit usando el `PriceStep` del instrumento multiplicado por `StopLossPoints` y `TakeProfitPoints`, respectivamente. Estos offsets recrean los multiplicadores `_Point` en la versión MQL.
8. En cada vela terminada, sale de las posiciones cuando el bid/ask rastreado alcanza el nivel de stop-loss o take-profit.

## Parámetros
| Parámetro | Descripción | Por defecto |
|-----------|-------------|-------------|
| `FastMaPeriod` | Longitud de la SMA rápida que impulsa la señal de cruce. | 25 |
| `SlowMaPeriod` | Longitud de la SMA lenta que actúa como filtro de tendencia. | 30 |
| `StopLossPoints` | Distancia del stop expresada en pasos de precio (`PriceStep * StopLossPoints`). | 30 |
| `TakeProfitPoints` | Objetivo de beneficio expresado en pasos de precio (`PriceStep * TakeProfitPoints`). | 100 |
| `ResistanceThreshold` | Número mínimo de ocurrencias requeridas para que un nivel de precio sea tratado como un clúster de liquidez. | 15 |
| `HistoryDepth` | Número de velas recientes almacenadas para la detección de clústeres (establecer en 100 para pares de oro como en el EA original). | 500 |
| `LevelTolerance` | Distancia máxima permitida entre el bid/ask actual y un nivel de clúster. | 0.0005 |
| `CandleType` | Serie de velas procesada por la estrategia (marco temporal o tipo personalizado). | Marco temporal de 1 minuto |

## Notas de implementación
- La suscripción al libro de órdenes se utiliza para capturar los mejores precios bid/ask actualizados, coincidiendo con la ejecución basada en ticks en el experto MQL.
- El cálculo de clústeres evita LINQ y almacena resultados en buffers reutilizables para respetar las directrices de conversión de StockSharp.
- Los objetivos de stop y take-profit se gestionan internamente porque las estrategias StockSharp ejecutan órdenes sintéticas en lugar de órdenes pendientes del lado del broker.
- Los helpers de gráficos dibujan velas, ambas SMAs y operaciones ejecutadas para verificación visual durante las pruebas.

## Consejos de uso
- Aumentar `HistoryDepth` cuando se trabaja en marcos temporales más altos para mantener un tamaño de muestra significativo para el clustering de niveles.
- Ajustar `LevelTolerance` en instrumentos con tamaños de tick pequeños para evitar clústeres no relacionados.
- Reducir `ResistanceThreshold` en mercados ilíquidos donde se esperan menos repeticiones.
- El parámetro de volumen predeterminado de la clase base `Strategy` controla el tamaño de la orden; ajustarlo en el entorno de alojamiento o sobreescribirlo antes de iniciar la estrategia.
