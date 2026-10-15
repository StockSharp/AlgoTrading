# Estrategia Currencyprofits de Canal Alto-Bajo
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port en StockSharp del asesor experto de MetaTrader `Currencyprofits_01.1`. Combina un filtro de tendencia de medias móviles rápida/lenta con un rompimiento del extremo del canal reciente. Cuando la media móvil rápida está por encima de la lenta, la estrategia espera un entorno alcista y espera que el precio reteste el mínimo más bajo de la ventana del canal anterior. Las operaciones cortas se realizan cuando la media rápida está por debajo de la lenta y el precio retesta el máximo más alto del canal.

La implementación funciona en cualquier instrumento que proporcione datos de velas. Todos los cálculos se realizan en velas cerradas para garantizar estabilidad tanto en backtests como en operativa en vivo.

## Lógica de trading
1. Suscribirse al tipo de vela configurado y calcular dos medias móviles y un canal de estilo Donchian basado en las anteriores `ChannelLength` velas (por defecto 6 barras).
2. Almacenar los valores anteriores de las velas de los indicadores para imitar la lógica MQL original que usa un desplazamiento de una barra.
3. **Entrada larga**: cuando la MA rápida anterior es mayor que la MA lenta anterior y el mínimo de la vela actual toca o rompe el mínimo del canal anterior.
4. **Entrada corta**: cuando la MA rápida anterior es menor que la MA lenta anterior y el máximo de la vela actual toca o rompe el máximo del canal anterior.
5. **Reglas de salida**:
   - Cerrar posiciones largas si la siguiente vela cierra por encima del máximo del canal almacenado o si se alcanza el stop protector.
   - Cerrar posiciones cortas si la siguiente vela cierra por debajo del mínimo del canal almacenado o si se activa el stop-loss.
6. Solo una posición está activa a la vez; la estrategia ignora nuevas señales mientras una operación está abierta.

## Dimensionamiento de posición
- `RiskPercent` define la fracción del valor del portafolio que puede arriesgarse por operación (por defecto `0.14`, es decir, 14%).
- La distancia del stop-loss se deriva de `StopLossPoints` multiplicada por el `PriceStep` del instrumento (o puntos si no hay metadatos disponibles).
- El riesgo en efectivo por contrato se estima con el valor de paso de intercambio (`StepPrice`). Si el instrumento no expone esta información, se usa la distancia de precio bruta.
- El volumen final de la orden se alinea a las restricciones de trading del instrumento (`VolumeStep`, `MinVolume`, `MaxVolume`). Si el dimensionamiento basado en riesgo no puede calcularse, se usa el `Volume` base de la estrategia.

## Parámetros
- `FastLength` – longitud de la media móvil rápida usada para detectar la tendencia (por defecto 32).
- `FastMaType` – tipo de la media móvil rápida (Simple, Exponential, Smoothed, Weighted).
- `SlowLength` – longitud de la media móvil lenta (por defecto 86).
- `SlowMaType` – tipo de la media móvil lenta.
- `PriceSource` – precio de vela aplicado a ambas medias móviles (por defecto Close).
- `ChannelLength` – número de velas anteriores que forman el canal alto/bajo (por defecto 6).
- `StopLossPoints` – distancia del stop expresada en puntos del instrumento antes de convertirse a precio (por defecto 170).
- `RiskPercent` – fracción del capital arriesgado por operación (por defecto 0.14 → 14%).
- `CandleType` – marco temporal de las velas usadas para todos los cálculos (por defecto 1 hora, puede cambiarse para coincidir con el período del gráfico deseado).

## Notas de uso
- Asegurarse de que `Security.PriceStep`, `Security.StepPrice` y los metadatos de volumen estén completos para un dimensionamiento preciso de la posición.
- Establecer el `Volume` de la estrategia a un valor de respaldo razonable cuando el dimensionamiento basado en riesgo esté deshabilitado (p. ej., `RiskPercent = 0`).
- La lógica opera en velas cerradas; las ejecuciones en vivo ocurren al cierre de la barra que confirma la señal.
- El stop-loss se gestiona internamente; no hay take-profit separado, lo que refleja el asesor experto fuente.

## Fuente
Convertido de `MQL/17641/Currencyprofits_01.1.mq5` con énfasis en legibilidad y compatibilidad con la API de alto nivel de StockSharp.
