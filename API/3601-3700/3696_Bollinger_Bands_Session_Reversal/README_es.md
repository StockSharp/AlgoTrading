# Bollinger Inversión de sesión de bandas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es una adaptación de C# del asesor experto MetaTrader **BollingerBandsEA (ver. 3.0)**. Negocia configuraciones de reversión a la media que ocurren después de que el precio se extiende más allá de las bandas Bollinger durante la sesión de negociación activa.

## Lógica comercial

1. Suscríbase a la serie principal de velas intradiarias (velas de 15 minutos de forma predeterminada) y a una serie de velas diarias utilizadas para crear el filtro de tendencias.
2. Calcule Bollinger Bandas (largo 20, ancho 2.0) en la serie intradiaria y un SMA de 100 períodos en cierres diarios.
3. Realice un seguimiento de los máximos y mínimos del día actual y anterior, y mantenga los valores de banda Bollinger anteriores para la evaluación de la señal.
4. Solo permita entradas dentro de la ventana de la sesión de negociación: desde `SessionStartOffsetMinutes` después de la apertura del día de negociación hasta `SessionEndOffsetMinutes` antes del final del día de negociación.
5. Omita las operaciones una vez que el PnL acumulado del día actual se vuelva positivo, imitando la parada diaria de EA.
6. Entre en corto cuando la vela anterior sea bajista, cierre por encima de la banda superior, el cierre actual permanezca por encima de esa banda, el ancho de la banda sea lo suficientemente ancho, el precio esté por debajo del SMA diario y el precio se negocie por encima del máximo diario actual o anterior.
7. Entre en largo cuando la vela anterior sea alcista, cierre por debajo de la banda inferior, el cierre actual permanezca por debajo de esa banda, el ancho de la banda sea lo suficientemente ancho, el precio esté por encima del SMA diario y el precio se negocie por debajo del mínimo diario actual o anterior.
8. El tamaño de la posición está determinado por el volumen fijo configurado o por el tamaño basado en el riesgo que utiliza la distancia hasta el stop-loss en puntos.
9. Las salidas se realizan comprobando el stop-loss, el take-profit, el cierre opcional en la banda media, un trailing stop opcional y la lógica de equilibrio opcional. Las operaciones perdedoras también se pueden liquidar después de un tiempo de espera configurable.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| `CandleType` | Serie de velas intradía utilizadas para el comercio. |
| `BollingerLength` | Periodo de la media móvil de las Bollinger Bandas. |
| `BollingerWidth` | Multiplicador de ancho de las Bollinger Bandas. |
| `DailyMaLength` | Duración del filtro diario SMA. |
| `StopLossPoints` | Distancia de stop-loss expresada en puntos del instrumento. |
| `UseRiskVolume` | Permite dimensionar las posiciones en función del riesgo. |
| `RiskPercent` | Porcentaje de cuenta utilizado para el dimensionamiento basado en riesgos. |
| `FixedVolume` | Volumen fijo alternativo cuando el tamaño del riesgo está deshabilitado o no es posible. |
| `SessionStartOffsetMinutes` | Minutos después del inicio de la sesión antes de que se permitan las entradas. |
| `SessionEndOffsetMinutes` | Minutos antes de finalizar la sesión cuando se bloquean las entradas. |
| `CloseOnMiddleBand` | Salga de la posición cuando el precio cruce la banda media Bollinger. |
| `EnableTrailing` | Permite realizar ajustes de trailing stop. |
| `TrailingFactor` | Se requiere un multiplicador de distancia antes de seguir la parada. |
| `EnableBreakEven` | Permite mover el stop al precio de entrada. |
| `BreakEvenFactor` | Múltiplo de beneficio necesario para mover el stop al punto de equilibrio. |
| `CloseLosingAfterMinutes` | Cierra las operaciones perdedoras después de mantenerlas durante los minutos especificados. |

## Notas

- Las órdenes protectoras de stop-loss y take-profit se simulan comprobando los extremos de las velas en cada actualización. Ajuste esta sección si se requieren órdenes de protección del lado cambiario.
- El tamaño basado en el riesgo depende de `Security.Step` y `Security.StepPrice`. Si faltan estos valores, la estrategia volverá al volumen fijo.
- El stop de ganancias diario utiliza la estrategia PnL, por lo tanto, el PnL realizado y el flotante deben estar en la misma moneda que la cartera.
