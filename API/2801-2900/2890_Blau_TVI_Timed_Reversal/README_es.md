# Estrategia Blau TVI de Reversión Temporizada
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Resumen
- Convertida desde el asesor experto de MetaTrader 5 **Exp_BlauTVI_Tm.mq5** ubicado en `MQL/21014`.
- Reimplementa la lógica del Blau Tick Volume Index (TVI) con tres etapas de suavizado configurables.
- Genera operaciones de reversión cuando el TVI suavizado cambia de pendiente y opcionalmente restringe las órdenes a una sesión de trading definida por el usuario.
- Soporta protecciones opcionales de stop-loss y take-profit definidas en puntos de precio.

## Lógica del Blau Tick Volume Index
El experto original usa el indicador personalizado `BlauTVI` que cuenta las subidas y bajadas de volumen de tick y suaviza el resultado varias veces. El puerto en C# mantiene la misma idea:

1. **Conteo bruto de ticks al alza/baja**
   - `UpTicks = (Volume + (Close - Open) / PriceStep) / 2`
   - `DownTicks = Volume - UpTicks`
   - El volumen de vela se usa como aproximación del volumen de tick porque el feed de StockSharp no expone conteos de tick para velas agregadas.
2. **Suavizado Etapa 1** – `UpTicks` y `DownTicks` se suavizan con el tipo de media móvil seleccionado (`EMA`, `SMA`, `SMMA`, `WMA`, `JMA`) y longitud `Length1`.
3. **Suavizado Etapa 2** – las salidas de la etapa 1 se suavizan de nuevo con longitud `Length2`.
4. **Cálculo TVI** – `TVI = 100 * (Up2 - Down2) / (Up2 + Down2)`.
5. **Suavizado Etapa 3** – el TVI se suaviza una vez más con longitud `Length3` para reducir el ruido.

La estrategia almacena un breve historial deslizante de los valores finales de TVI para replicar el desplazamiento `SignalBar` usado por el EA original (`CopyBuffer` con desplazamiento `SignalBar`).

## Reglas de Trading
- **Detección de pendiente de señal**
  - Cuando el valor TVI anterior (`SignalBar + 1`) es menor que el valor más antiguo (`SignalBar + 2`), el TVI se considera girando hacia arriba. Si el último valor disponible también es mayor que el anterior, se produce una señal de reversión alcista.
  - Cuando el valor TVI anterior es mayor que el valor más antiguo, el TVI está girando hacia abajo. Si el último valor está por debajo del anterior, se produce una señal de reversión bajista.
- **Gestión de posiciones**
  - Las entradas en largo requieren `EnableBuyOpen = true`, la señal alcista anterior y una posición actual no positiva. La estrategia cierra cualquier posición corta existente antes de comprar agregando el tamaño corto absoluto al `Volume` configurado.
  - Las entradas cortas requieren `EnableSellOpen = true`, la señal bajista y una posición no negativa.
  - Las salidas en largo se activan cuando la pendiente del TVI gira bajista y `EnableBuyClose = true`.
  - Las salidas cortas se activan cuando la pendiente del TVI gira alcista y `EnableSellClose = true`.
- **Filtro de tiempo**
  - Cuando `EnableTimeFilter = true`, la estrategia solo abre nuevas posiciones dentro de la ventana [`StartHour`:`StartMinute`, `EndHour`:`EndMinute`]. Se admiten sesiones nocturnas (inicio > fin). Las posiciones se cierran forzosamente tan pronto como el tiempo sale de la sesión.
- **Protección**
  - `StopLossPoints` y `TakeProfitPoints` se convierten a distancias de precio absolutas multiplicando por el `PriceStep` del instrumento y se pasan a `StartProtection`. Establecer en cero para desactivar.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `Volume` | Tamaño de orden usado para cada entrada (se agregan contratos adicionales para cubrir la exposición opuesta). |
| `CandleType` | Tipo/marco temporal de datos de vela usado para todos los cálculos (predeterminado: marco temporal de 4 horas). |
| `MaType` | Tipo de media móvil para todas las etapas de suavizado (EMA, SMA, SMMA, WMA, JMA). |
| `Length1`, `Length2`, `Length3` | Longitudes de suavizado para cada etapa del cálculo Blau TVI. |
| `SignalBar` | Desplazamiento para los valores TVI usados en generación de señales (1 coincide con la vela cerrada anterior como la versión MQL). |
| `EnableBuyOpen`, `EnableSellOpen` | Permitir abrir posiciones largas/cortas en señales. |
| `EnableBuyClose`, `EnableSellClose` | Permitir cerrar posiciones largas/cortas existentes cuando la pendiente se revierte. |
| `EnableTimeFilter` | Activar/desactivar la ventana de sesión de trading. |
| `StartHour`, `StartMinute`, `EndHour`, `EndMinute` | Límites de sesión en hora de bolsa. Soporta rangos intradía y nocturnos. |
| `StopLossPoints`, `TakeProfitPoints` | Distancias protectoras fijas expresadas en puntos de precio (0 desactiva cada protección). |

## Notas de Implementación
- El entorno StockSharp no expone conteos de tick para velas agregadas, por lo que el volumen de vela se usa en lugar del volumen de tick. Esto mantiene el comportamiento cercano al indicador original mientras permanece compatible con los datos disponibles.
- La estrategia rastrea solo un historial TVI compacto (unos pocos valores más recientes) para reproducir el desplazamiento `SignalBar` sin violar la directriz del repositorio que desaconseja colecciones pesadas personalizadas.
- `StartProtection` se inicializa solo cuando hay un `PriceStep` válido disponible; de lo contrario, recurre a protección sin objetivos fijos.
- Todos los comentarios fueron reescritos en inglés para cumplir con las reglas del repositorio, y se usan tabulaciones para indentación según lo requerido por `AGENTS.md`.

## Consejos de Uso
1. Comenzar con el marco temporal H4 predeterminado y suavizado EMA, que coinciden con la configuración del asesor experto original.
2. Ajustar `SignalBar` a 0 si prefiere actuar en la última vela cerrada en lugar de esperar un barra, pero recuerde que esto se desvía de la lógica MQL.
3. Al ejecutar en instrumentos con horarios de trading irregulares, configurar el filtro de tiempo para evitar tomar señales durante períodos ilíquidos.
4. Combinar con gestión de dinero a nivel de portafolio si necesita dimensionamiento dinámico; `Volume` es estático por diseño, reflejando el enfoque de lote fijo del EA fuente.
