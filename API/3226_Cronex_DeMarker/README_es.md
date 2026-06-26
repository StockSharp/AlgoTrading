# Cronex DeMarker
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia Cronex DeMarker** reproduce el clásico asesor experto Cronex que combina el oscilador DeMarker con una pila de doble suavizado. Primero, los valores del DeMarker son suavizados por una media móvil simple rápida, luego el resultado es suavizado una vez más por una media más lenta. La distancia y el orden relativo de estas dos líneas proporcionan señales de entrada estilo reversión.

La implementación MQL5 original permite alternancias de dirección de trade y funciona en marcos temporales superiores. Este puerto StockSharp mantiene la misma filosofía: reacciona cuando la línea rápida cruza a través de la lenta y cierra inmediatamente cualquier posición opuesta. Debido a que el sistema es contrario, un cruce por debajo de la línea lenta abre una posición larga, mientras que un cruce por encima abre una corta. Ambas direcciones pueden desactivarse de forma independiente a través de parámetros, lo que hace que la estrategia sea flexible para diferentes asignaciones de cartera.

## Cómo funciona

1. Solicitar velas para el marco temporal seleccionado (4H por defecto).
2. Calcular el oscilador DeMarker y suavizarlo con una SMA rápida (por defecto 14 barras).
3. Aplicar una segunda SMA (por defecto 25 barras) sobre la línea rápida para obtener la línea de señal.
4. Cuando la línea rápida estaba por encima de la línea lenta en la vela anterior y ahora cae por debajo, la estrategia compra (reversión contraria). Cualquier posición corta existente se aplana.
5. Cuando la línea rápida estaba por debajo de la línea lenta en la vela anterior y ahora sube por encima, la estrategia vende y cierra cualquier posición larga abierta.
6. El tamaño de la posición se define por la propiedad `Volume`; las reversiones usan la posición absoluta para invertir inmediatamente.

Esta lógica permite al experto capturar movimientos de agotamiento a corto plazo después de fuertes empujes de momentum, convirtiéndolo en una herramienta de reversión a la media que prefiere mercados en rango o choppy.

## Parámetros predeterminados

| Parámetro | Valor predeterminado | Descripción |
|-----------|---------|-------------|
| `DeMarkerPeriod` | 25 | Número de barras utilizadas por el oscilador DeMarker. |
| `FastPeriod` | 14 | Longitud de la primera SMA de suavizado aplicada a los valores del DeMarker. |
| `SlowPeriod` | 25 | Longitud de la SMA de señal aplicada a la línea rápida. |
| `CandleType` | 4 horas | Serie de velas utilizada para los cálculos del indicador. |
| `EnableLongEntry` | true | Permitir entradas largas contrarias cuando la línea rápida cruza por debajo de la línea lenta. |
| `EnableShortEntry` | true | Permitir entradas cortas cuando la línea rápida cruza por encima de la línea lenta. |
| `EnableLongExit` | true | Cerrar posiciones largas existentes cuando aparecen condiciones bajistas. |
| `EnableShortExit` | true | Cerrar posiciones cortas existentes cuando aparecen condiciones alcistas. |

## Filtros y etiquetas

- **Categoría**: Reversión a la media, basado en Oscilador
- **Dirección**: Largo y Corto (configurable)
- **Indicadores**: DeMarker, Media Móvil Simple (doble suavizado)
- **Stops**: Ninguno (totalmente impulsado por señal)
- **Marco temporal**: Trading Swing (H4 por defecto, ajustable)
- **Complejidad**: Intermedio debido a la cadena de indicadores secuenciales
- **Perfil de riesgo**: Medio — las entradas contrarias pueden enfrentar tendencias extendidas
- **Automatización**: Totalmente automatizado a través de la API de alto nivel de StockSharp

## Notas de uso

- La estrategia solo procesa velas terminadas para evitar problemas de repintado.
- Las órdenes de reversión reutilizan el tamaño de posición absoluto, garantizando aplanamiento inmediato antes de entrar en la nueva dirección.
- La salida del gráfico dibuja las dos líneas suavizadas y los marcadores de trade, ayudando con la validación discrecional.
- Para carteras que solo permiten una dirección, deshabilitar las entradas y salidas no deseadas a través de los parámetros proporcionados.
- Considerar agregar controles de riesgo externos (stop-loss, salida trailing) al implementar en activos volátiles.
