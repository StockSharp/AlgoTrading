# Estrategia BullsBearsEyes EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción
Esta estrategia es un port de StockSharp del **BullsBearsEyes EA** para MetaTrader 5. Reconstruye el indicador personalizado combinando los osciladores clásicos Bulls Power y Bears Power con el mismo suavizado IIR de cuatro etapas usado en el código original. La relación resultante oscila entre 0 y 1 y refleja el dominio de vendedores o compradores. Cuando la relación cae a **0**, el mercado se considera agotado por los osos y la estrategia prepara una entrada larga. Cuando la relación sube a **1**, la presión alcista se considera agotada y la estrategia busca una entrada corta. Todos los cálculos se realizan únicamente en velas totalmente cerradas, replicando la implementación MQL que evaluaba `custom[1]` al nacer de cada nueva barra.

## Lógica de trading
1. Suscribirse a la serie de velas configurada y vincular los indicadores Bulls Power y Bears Power.
2. En cada vela terminada, los valores del indicador se procesan a través de la misma cascada de suavizado IIR (`L0` – `L3`) que el EA original.
3. Se computa la relación `CU / (CU + CD)`. Una secuencia puramente bajista hace que `CU` sea igual a cero, mientras que una puramente alcista fuerza a `CD` a cero.
4. La estrategia almacena la relación de la vela anterior y la usa como señal accionable:
   - Relación anterior igual a **0** ⇒ cerrar posiciones cortas y abrir una posición larga.
   - Relación anterior igual a **1** ⇒ cerrar posiciones largas y abrir una posición corta.
   - Las relaciones intermedias se ignoran para mantenerse fiel al código fuente.
5. Las órdenes se envían con el valor `Volume` actual y automáticamente netean la posición opuesta antes de abrir una nueva.

## Gestión de riesgo
- **Stop Loss / Take Profit** – expresados en pips, traducidos a precios absolutos con detección de tamaño de pip idéntica a la implementación MT5 (los instrumentos de 5 y 3 dígitos se manejan mediante el multiplicador de paso).
- **Trailing Stop / Trailing Step** – lógica idéntica: una vez que el precio avanza `TrailingStop + TrailingStep`, el stop se mueve para mantener una distancia `TrailingStop` constante desde el cierre actual. Las posiciones largas y cortas se gestionan simétricamente.
- Los niveles protectores se recalculan cuando cambia la posición neta, asegurando que se use el precio promedio de posición para cálculos adicionales.
- La estrategia cierra la posición completa cuando un nivel protector es superado dentro del rango de la vela actual.

## Filtro de sesión
El filtro de tiempo opcional coincide con las entradas del asesor experto:
- `Use Time Control` – habilita/deshabilita el filtro.
- `Start Hour` – hora de inicio inclusiva (0–23).
- `End Hour` – hora de finalización exclusiva (0–23). Si la hora de finalización es menor que la de inicio, la sesión se extiende sobre la medianoche.
Durante las horas restringidas, la estrategia se abstiene de abrir nuevas posiciones pero sigue gestionando stops y trailing para operaciones existentes.

## Parámetros
| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `Period` | Longitud de promediado para Bulls/Bears Power. | 13 |
| `Gamma` | Factor de suavizado usado por el filtro de cuatro etapas (0–1). | 0.6 |
| `StopLossPips` | Distancia de stop-loss medida en pips. | 150 |
| `TakeProfitPips` | Distancia de take-profit medida en pips. | 150 |
| `TrailingStopPips` | Distancia de trailing stop en pips (0 deshabilita el trailing). | 25 |
| `TrailingStepPips` | Avance mínimo antes de que el trailing stop pueda moverse. | 5 |
| `UseTimeControl` | Habilita el filtro de sesión de trading. | true |
| `StartHour` | Primera hora de trading (inclusiva). | 10 |
| `EndHour` | Última hora de trading (exclusiva). | 16 |
| `CandleType` | Tipo de vela/marco temporal usado para análisis. | Velas de 1 hora |

## Notas adicionales
- La API de alto nivel `SubscribeCandles().Bind(...)` se usa para replicar los cálculos originales sin recolectar velas manualmente.
- Los valores del indicador se procesan solo después de que la vela cierra (`CandleStates.Finished`).
- La detección del tamaño de pip recurre a `1` si el paso del instrumento no está disponible, permitiendo que la estrategia ejecute en entornos de prueba sintéticos.
- Los comentarios en el archivo C# explican cada sección lógica para un mantenimiento más fácil y comparación con la fuente MQL.
