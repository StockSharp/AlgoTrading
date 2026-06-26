# Estrategia XDeMarker Histogram Vol Direct
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el experto de MetaTrader 5 **Exp_XDeMarker_Histogram_Vol_Direct** usando la API de alto nivel de StockSharp. Multiplica el oscilador XDeMarker por el flujo de volumen elegido, suaviza tanto el oscilador como el volumen con el mismo promedio móvil, y compara el resultado con niveles superiores/inferiores configurables. Las decisiones de trading se toman cuando el histograma suavizado cambia de dirección entre barras consecutivas.

## Lógica del indicador

1. Calcular el oscilador XDeMarker clásico en el marco temporal seleccionado.
2. Escalar el oscilador por el conteo de ticks o el volumen real para cada vela finalizada.
3. Suavizar tanto el histograma como el volumen con el tipo de promedio móvil seleccionado.
4. Multiplicar el volumen suavizado por los multiplicadores de nivel configurados para obtener cuatro bandas dinámicas.
5. Detectar la dirección del histograma (subiendo o bajando). Cuando la dirección cambia, la estrategia abre una nueva posición en la dirección correspondiente mientras también cierra cualquier operación opuesta.

El método de suavizado soporta promedios móviles simples, exponenciales, suavizados (RMA/SMMA) y ponderados. Los filtros exóticos de la biblioteca original (JJMA, JurX, ParMA, T3, VIDYA, AMA) no están disponibles en este port.

## Reglas de trading

- **Entrada larga** — habilitada cuando `Allow Long Entry = true`. Si la barra anterior tenía dirección "arriba" y la última barra cambió a "abajo", la estrategia apunta a una posición larga de `Volume` lotes.
- **Entrada corta** — habilitada cuando `Allow Short Entry = true`. Activada cuando la barra anterior era "abajo" y la última barra gira "arriba".
- **Salida larga** — habilitada cuando `Allow Long Exit = true`. Si la dirección de la barra anterior es "abajo", la posición se liquida a menos que se dispare una nueva entrada corta en la misma barra.
- **Salida corta** — habilitada cuando `Allow Short Exit = true`. Activada cuando la dirección de la barra anterior es "arriba".

Las señales se evalúan una vez por vela finalizada. La implementación de StockSharp mantiene el retardo original de una barra; el parámetro `Signal Bar` está presente como referencia pero los valores distintos de `1` se ignoran con una advertencia.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| Candle Type | Marco temporal utilizado para construir velas para el indicador. |
| DeMarker Period | Período de retrospectiva para el oscilador XDeMarker base. |
| Volume Source | Elegir entre conteo de ticks y volumen real negociado. |
| High Level 2 / High Level 1 | Multiplicadores aplicados al volumen suavizado para formar bandas superiores. |
| Low Level 1 / Low Level 2 | Multiplicadores para bandas inferiores. |
| Smoothing Method | Tipo de promedio móvil aplicado tanto al histograma como al volumen. |
| Smoothing Length | Longitud de la ventana de suavizado. |
| Smoothing Phase | Marcador de posición de compatibilidad (no se usa pero se mantiene para paridad). |
| Signal Bar | Desplazamiento histórico, fijo en 1 igual que en el experto. |
| Allow Long/Short Entry | Habilitar la apertura de posiciones en la dirección respectiva. |
| Allow Long/Short Exit | Habilitar el cierre automático de operaciones existentes. |

## Notas de implementación

- La clase `XDeMarkerHistogramVolDirectIndicator` reproduce los buffers del indicador MT5 y expone el histograma suavizado, las bandas y los flags de dirección a través de un valor de indicador complejo.
- Cuando se requiere una nueva exposición objetivo, la estrategia envía una única orden de mercado que mueve la posición actual al nivel deseado (`Volume`, `-Volume` o plano). Esto imita las llamadas secuenciales de cierre/apertura en el código MQL5 original sin duplicar órdenes.
- El renderizado del gráfico traza automáticamente las velas, el indicador personalizado y las operaciones ejecutadas cuando hay un área de gráfico disponible.
