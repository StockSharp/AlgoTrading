# Estrategia de Inicio (Starter)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de Inicio (Starter)** es una conversión del experto MetaTrader 5 "Starter (barabashkakvn's edition)". El sistema espera
que el Índice de Canal de Materias Primas (CCI) rebote desde territorio de sobreventa o sobrecompra extrema y confirma el movimiento
con la pendiente de una media móvil a largo plazo. Cuando el momentum coincide con el filtro de tendencia, la estrategia abre una sola
posición de mercado cuyo tamaño se determina por un porcentaje de riesgo configurable del portafolio. Los stops de protección y un
mecanismo de trailing opcional reproducen las reglas de gestión de dinero del experto original.

## Lógica de Trading

- **Filtro de tendencia** — una media móvil (MA) configurable debe subir más rápido que `MaDelta` para permitir operaciones largas y
  caer más rápido que `MaDelta` para permitir operaciones cortas. La estrategia admite los mismos métodos de suavizado que la versión
  MQL (simple, exponencial, suavizada, ponderada linealmente).
- **Confirmación CCI** — el Índice de Canal de Materias Primas debe cruzar nuevamente por encima de `-CciLevel` desde abajo para
  activar entradas largas y cruzar por debajo de `CciLevel` desde arriba para activar cortos. El indicador se evalúa solo en velas
  cerradas, emulando el procesamiento barra a barra del original.
- **Modelo de posición única** — el algoritmo mantiene como máximo una posición abierta. Las nuevas señales se ignoran hasta que la
  operación actual se cierra, coincidiendo con la lógica de MetaTrader que filtra por número mágico y símbolo.

### Reglas de Entrada

1. Esperar al cierre de una vela.
2. Calcular los últimos y anteriores valores de la media móvil en los desplazamientos configurados.
3. Calcular las lecturas actuales y anteriores de CCI.
4. **Ir en largo** cuando:
   - La pendiente de la media móvil supera `MaDelta` (MA actual menos MA anterior).
   - El valor actual de CCI es mayor que el anterior.
   - El CCI cruza hacia arriba por `-CciLevel` (el anterior por debajo del umbral, el actual por encima).
5. **Ir en corto** cuando:
   - La pendiente de la media móvil está por debajo de `-MaDelta`.
   - El valor actual de CCI es menor que el anterior.
   - El CCI cruza hacia abajo por `CciLevel` (el anterior por encima del umbral, el actual por debajo).

### Reglas de Salida

- **Stop-loss inicial** — si `StopLossPips` es mayor que cero, el precio de entrada ejecutado se desplaza por `StopLossPips * PriceStep`
  para calcular un stop de protección inicial.
- **Trailing stop** — cuando tanto `TrailingStopPips` como `TrailingStepPips` son positivos, el stop se avanza siempre que el precio
  mejore al menos el paso configurado. Las operaciones largas mueven el stop a `Close - TrailingStop`, las cortas a `Close + TrailingStop`.
- **Salida manual** — si el precio toca el nivel del stop dentro del rango de la vela, la estrategia cierra la posición con una orden
  a mercado y restablece el estado de protección.

## Gestión de Riesgo

- **Dimensionamiento de posición** — el volumen base es `Portfolio.CurrentValue * MaximumRisk / price`. Cuando el bróker o el back-end
  reporta un valor de capital inválido, la estrategia recurre a la propiedad `Volume` manual (predeterminado 1).
- **Reducción por racha de pérdidas** — después de dos o más operaciones perdedoras consecutivas, el volumen se reduce por
  `volume * losses / DecreaseFactor`, imitando la regla original de `DecreaseFactor`. Cualquier operación ganadora reinicia el
  contador de pérdidas.

## Parámetros

| Parámetro | Predeterminado | Descripción |
|-----------|----------------|-------------|
| `MaximumRisk` | `0.02` | Fracción del capital arriesgado por operación al dimensionar la posición. |
| `DecreaseFactor` | `3` | Divisor de reducción de lote aplicado después de dos o más operaciones perdedoras consecutivas. |
| `CciPeriod` | `14` | Número de barras usadas por el Índice de Canal de Materias Primas. |
| `CciLevel` | `100` | Umbral de sobreventa/sobrecompra para cruzamientos de CCI. |
| `CciCurrentBar` | `0` | Desplazamiento del valor actual de CCI (0 = última vela). |
| `CciPreviousBar` | `1` | Desplazamiento del valor anterior de CCI. |
| `MaPeriod` | `120` | Período del filtro de tendencia de la media móvil. |
| `MaMethod` | `Simple` | Método de suavizado de la media móvil (Simple, Exponential, Smoothed, LinearWeighted). |
| `MaCurrentBar` | `0` | Desplazamiento aplicado al valor de la media móvil. |
| `MaDelta` | `0.001` | Diferencia de pendiente mínima entre lecturas actual y anterior de MA. |
| `StopLossPips` | `0` | Distancia del stop-loss inicial en pips (0 deshabilita el stop). |
| `TrailingStopPips` | `5` | Distancia base del trailing stop en pips (0 deshabilita el trailing). |
| `TrailingStepPips` | `5` | Mejora mínima en pips antes de avanzar el trailing stop. |
| `CandleType` | Marco temporal `30m` | Suscripción de velas principal procesada por la estrategia. |

## Notas de Implementación

- Los búferes del indicador se almacenan en caché internamente para que la estrategia pueda acceder a valores históricos con desplazamientos
  arbitrarios, replicando el enfoque MQL de indexar arreglos de indicadores.
- El tamaño del pip se deriva de `Security.PriceStep`. Si el instrumento no reporta un paso de precio válido, las distancias de stop
  y trailing se tratan como cero.
- Todos los comentarios dentro del código están escritos en inglés según las pautas del repositorio.
- La versión Python se omite intencionalmente según lo solicitado.
