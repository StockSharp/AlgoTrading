# Estrategia de Cruce Porcentual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia replica el comportamiento del experto original de MetaTrader `Exp_PercentageCrossover`. Opera en la dirección del indicador Percentage Crossover, que traza una línea de seguimiento de precio que sólo puede moverse dentro de una banda porcentual fija alrededor del cierre actual. La pendiente de esta línea define el estado del mercado y dispara las operaciones.

## Concepto

1. En cada vela completada el indicador conserva el valor anterior de la línea.
2. Una actualización alcista se produce cuando el cierre empuja la línea de seguimiento por encima de su valor anterior al menos en `percent` por ciento del precio.
3. Una actualización bajista se produce cuando el cierre arrastra la línea de seguimiento por debajo de su valor anterior en el mismo porcentaje.
4. Si el cierre permanece dentro de la banda, la línea se mantiene plana y conserva su último color.

El color de la línea se interpreta de la misma forma que en MetaTrader:

- **Índice de color 0 (azul/violeta)** – la línea sube (contexto alcista).
- **Índice de color 1 (naranja)** – la línea baja (contexto bajista).

## Reglas de trading

### Entradas largas
- Habilitadas solo cuando `BuyPosOpen = true`.
- Se evalúa la barra seleccionada por `SignalBar` (1 significa la última barra cerrada).
- Se abre una posición larga cuando esa barra cambia del color 1 al color 0.

### Entradas cortas
- Habilitadas solo cuando `SellPosOpen = true`.
- Se evalúa la misma barra `SignalBar`.
- Se abre una posición corta cuando la barra cambia del color 0 al color 1.

### Gestión de posiciones
- Si `BuyPosClose = true`, cualquier posición larga abierta se cierra cuando la barra actual (tras aplicar el desplazamiento `SignalBar`) es de color 1.
- Si `SellPosClose = true`, cualquier posición corta abierta se cierra cuando esa barra es de color 0.
- Cuando `UseTimeFilter = true` y la hora actual está fuera de la ventana de trading configurada, la estrategia sale inmediatamente de la posición activa e ignora nuevas señales hasta que el mercado vuelva a entrar en la ventana.
- Las órdenes se envían con `BuyMarket()` y `SellMarket()`. La cantidad real proviene de la propiedad `Volume` de la estrategia.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|----------------|
| `Percent` | Banda porcentual para la línea de seguimiento. Valores más altos hacen que la línea reaccione más lentamente. | `1` |
| `SignalBar` | Qué barra cerrada se analiza (1 = última cerrada). Debe mantenerse positivo. | `1` |
| `BuyPosOpen` / `SellPosOpen` | Habilitar entradas largas o cortas respectivamente. | `true` |
| `BuyPosClose` / `SellPosClose` | Habilitar la lógica de cierre para posiciones largas o cortas. | `true` |
| `UseTimeFilter` | Activar la ventana de trading. | `true` |
| `StartHour` / `StartMinute` | Hora y minuto que abren la ventana de trading cuando el filtro está activo. | `0` / `0` |
| `EndHour` / `EndMinute` | Hora y minuto que cierran la ventana de trading. | `23` / `59` |
| `CandleType` | Marco temporal de las velas usadas para el indicador y las señales. | `4h` |

## Notas

- El filtro de tiempo sigue estrictamente el Asesor Experto original. Cuando la hora de inicio es mayor que la hora de fin, la lógica crea una ventana nocturna, pero aún requiere que los minutos sean mayores o iguales a `StartMinute` para que la sesión se vuelva activa.
- `SignalBar` se evalúa únicamente en velas finalizadas. Establézcalo en `1` para reflejar la configuración predeterminada de MetaTrader.
- La estrategia no impone niveles de stop-loss ni take-profit. El control del riesgo debe gestionarse externamente o ajustando el porcentaje y la ventana de trading.
