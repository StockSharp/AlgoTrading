# Estrategia Two Per Bar
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
El experto original de MetaTrader "Two PerBar" abre una posición larga y una corta al comienzo de cada nueva barra, cierra toda la cesta en la siguiente barra y opcionalmente aplica un multiplicador de volumen similar al martingala. El puerto de StockSharp mantiene el mismo ritmo rastreando explícitamente ambas patas cubiertas y reaccionando una vez por vela finalizada. Todas las órdenes se crean a través de la API de alto nivel y respetan los metadatos del instrumento (paso de precio, paso de volumen y restricciones de lote mínimo/máximo).

## Ciclo de trading
1. **Detección de nueva vela** – la estrategia se suscribe a la serie de velas configurada a través de `SubscribeCandles`. Cuando la vela llega con `State == CandleStates.Finished`, ha comenzado una nueva barra y el ciclo se ejecuta.
2. **Evaluar hits de take-profit** – cada pata almacenada lleva su propio precio de entrada y nivel de take-profit. Si el máximo o mínimo de la vela completada alcanza ese nivel, la pata se cierra inmediatamente con una orden de mercado y se elimina de la lista de seguimiento.
3. **Liquidación forzada de sobrantes** – cualquier pata que sobreviva el escaneo de take-profit se liquida al mercado antes de abrir el siguiente par. Esto refleja el código de MetaTrader que llama a `PositionClose` en cada apertura de barra.
4. **Determinar el siguiente tamaño de lote** –
   - Cuando un ciclo anterior todavía tenía patas abiertas, el mayor volumen entre ellas se multiplica por `VolumeMultiplier`.
   - Cuando la cesta terminó plana (por ejemplo, ambas patas alcanzaron su take-profit), el ciclo se reinicia a `InitialVolume`.
   - `PrepareVolume` normaliza el lote candidato redondeando a dos decimales, ajustándolo al `VolumeStep` del instrumento, verificando contra el `MinVolume` de la bolsa, y finalmente reiniciando a `InitialVolume` si supera el `MaxVolume` definido por el usuario o el `Security.MaxVolume`.
5. **Actualizar valores predeterminados** – el lote calculado se almacena dentro de `_lastCycleVolume` y se escribe en `Strategy.Volume` para que los métodos auxiliares reutilicen la misma cantidad.
6. **Generar un nuevo par cubierto** – `BuyMarket(volume)` abre la pata larga y `SellMarket(volume)` abre la pata corta. Cada pata recuerda el precio de cierre de la vela finalizada y el nivel absoluto de take-profit (`entry ± TakeProfitPoints * pointSize`). Un `TakeProfitPoints` cero o negativo deshabilita el take-profit y solo el paso de liquidación forzada cerrará la cesta.

El resultado es un straddle perpetuo: cada vela comienza con un par largo + corto, ambos se inspeccionan para objetivos de beneficio durante la barra, y todo queda plano antes del siguiente ciclo.

## Gestión del dinero y protección
- **Escalado similar al martingala** – `VolumeMultiplier` replica el multiplicador de MetaTrader. Cuando cualquier pata sobrevive hasta el paso de liquidación forzada, el siguiente ciclo usa el tamaño de la pata más grande multiplicado por este valor. Un ciclo profitable completado (ambas patas cerradas vía take-profit) reinicia el lote a `InitialVolume`.
- **Tope de volumen** – `MaxVolume` es un tope duro que fuerza el lote de vuelta a `InitialVolume` una vez que el multiplicador lo superaría. El mismo reinicio ocurre si el instrumento reporta un `Security.MaxVolume` más restrictivo.
- **Cumplimiento de la bolsa** – todos los volúmenes se ajustan al `VolumeStep` del valor y se rechazan cuando caen por debajo de `MinVolume`. Establecer `InitialVolume` en un tamaño negociable garantiza que la ruta de reinicio siempre permanezca válida.
- **Cálculo de puntos** – el desplazamiento de take-profit usa `Security.PriceStep` (o `MinPriceStep` como respaldo). Los instrumentos sin un paso definido efectivamente deshabilitan el take-profit porque el desplazamiento calculado es cero.

## Parámetros
| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `CandleType` | `DataType` | Marco temporal de 1 minuto | Marco temporal principal que activa el flujo de trabajo una vez por barra. |
| `InitialVolume` | `decimal` | `1` | Tamaño de lote usado al iniciar un nuevo ciclo sin patas supervivientes. |
| `VolumeMultiplier` | `decimal` | `2` | Multiplicador aplicado a la pata superviviente más grande del ciclo anterior. |
| `MaxVolume` | `decimal` | `10` | Tamaño máximo de lote permitido antes de reiniciar a `InitialVolume`. |
| `TakeProfitPoints` | `int` | `50` | Distancia en puntos de precio usada para construir el objetivo de take-profit por pata. `0` deshabilita el take-profit y se basa únicamente en la liquidación al cierre de barra. |

## Notas de implementación y diferencias
- Las patas cubiertas se rastrean manualmente dentro de `_legs` para que la estrategia pueda razonar sobre exposiciones largas/cortas individuales aunque StockSharp reporte solo la posición neta.
- En lugar de depender de ticks individuales, la lógica de take-profit verifica el rango alto/bajo de la vela completada. Esto mantiene la implementación determinista mientras permanece fiel al comportamiento original "por barra".
- Los ajustes de deslizamiento y número mágico de MetaTrader no están expuestos; StockSharp maneja los detalles de enrutamiento de órdenes, y la estrategia se ejecuta en el portfolio asociado con la instancia de estrategia padre.
- La colocación de órdenes usa los métodos auxiliares de `Strategy` (`BuyMarket`, `SellMarket`) sin agregar indicadores directamente a `Strategy.Indicators`, cumpliendo con las pautas del repositorio.

## Consejos de uso
- Ajuste `InitialVolume` al paso de lote del instrumento antes de iniciar la estrategia. El constructor no intenta redondear automáticamente su entrada.
- Si el instrumento tiene un paso de precio muy pequeño, considere reducir `TakeProfitPoints`; de lo contrario, el take-profit calculado puede ubicarse irrealistamente lejos.
- Dado que la estrategia abre órdenes en dirección opuesta al mismo tiempo, ejecútela en conectores/bolsas que permitan posiciones cubiertas. En entornos que netan posiciones inmediatamente, la lista `_legs` aún refleja la lógica pretendida, pero el comportamiento real del broker puede diferir.
- Agregue la estrategia a un gráfico para visualizar velas y operaciones ejecutadas (`DrawCandles` + `DrawOwnTrades` están habilitados en `OnStarted`).
