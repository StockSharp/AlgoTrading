# Estrategia Exp DEMA Canal de Rango Tm Plus
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Exp DEMA Range Channel Tm Plus porta el asesor experto MetaTrader original a la API de alto nivel de StockSharp. Construye un canal de media móvil exponencial doble (DEMA) alrededor de los extremos de precio e interpreta los colores de las velas producidos por el canal para decidir cuándo operar. La implementación mantiene la lógica de gestión de dinero simple, confiando en la propiedad `Volume` de la plataforma y órdenes de protección opcionales mientras reproduce las reglas de ruptura y tiempo de espera del código fuente.

## Lógica principal

- **Construcción del canal**
  - Se calculan dos indicadores DEMA con el mismo período: uno en los máximos de las velas y otro en los mínimos.
  - Sus salidas se desplazan hacia adelante un número configurable de barras (`Shift`) para coincidir con cómo el indicador personalizado original dibuja el canal.
  - Se puede agregar un desplazamiento de precio en puntos (`PriceShiftPoints`) para ensanchar o estrechar el canal.
- **Colores de señal**
  - Una vela que cierra por encima de la banda superior desplazada se considera alcista.
  - Una vela que cierra por debajo de la banda inferior desplazada se considera bajista.
  - La dirección del cuerpo de la vela (cierre ≥ apertura o cierre ≤ apertura) se preserva para imitar los cuatro colores posibles (0–3) del indicador MQL.
- **Condiciones de entrada**
  - La estrategia mira atrás `SignalBar` barras para evaluar el último color de ruptura y confirma que la barra anterior no ya mostró la misma señal. Esto captura el momento en que aparece una nueva ruptura.
  - Las entradas largas solo se permiten cuando `EnableBuyEntry` es verdadero y el color detectado corresponde a una ruptura hacia arriba.
  - Las entradas cortas requieren `EnableSellEntry` y una ruptura hacia abajo.
- **Condiciones de salida**
  - Las posiciones largas pueden cerrarse en cualquier ruptura hacia abajo si `EnableBuyExit` está habilitado.
  - Las posiciones cortas pueden cerrarse en rupturas hacia arriba si `EnableSellExit` está habilitado.
  - Las posiciones también pueden cerrarse después de un tiempo de mantenimiento configurable (`HoldingMinutes`) si `UseHoldingLimit` es verdadero, reflejando el filtro de tiempo del asesor experto.
- **Control de riesgo**
  - Las distancias opcionales de take-profit y stop-loss (en puntos de precio) activan `StartProtection`, que emite órdenes de protección usando ejecución de mercado cuando se alcanzan los umbrales.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `MaPeriod` | Período DEMA utilizado para las líneas del canal superior e inferior. |
| `Shift` | Número de barras que las líneas DEMA se desplazan hacia adelante antes de las comparaciones. |
| `PriceShiftPoints` | Distancia adicional, medida en puntos de precio (múltiplos de `PriceStep`), añadida a la línea superior y restada de la línea inferior. |
| `SignalBar` | Número de barras atrás usado para evaluar el color de ruptura. `0` significa barra actual, `1` la última barra cerrada, etc. |
| `EnableBuyEntry` / `EnableSellEntry` | Interruptor para entradas de ruptura largas y cortas. |
| `EnableBuyExit` / `EnableSellExit` | Interruptor para salir de posiciones largas o cortas en señales opuestas. |
| `UseHoldingLimit` | Habilita el cierre de posiciones después de `HoldingMinutes` minutos en el mercado. |
| `HoldingMinutes` | Tiempo máximo de mantenimiento antes de un cierre forzado; establecer en `0` para deshabilitar mientras se mantiene el flag verdadero. |
| `StopLossPoints` / `TakeProfitPoints` | Distancias de protección en puntos de precio. Cuando son mayores que cero se convierten en desplazamientos absolutos de precio y se pasan a `StartProtection`. |
| `CandleType` | Tipo de vela y marco temporal usado para todos los cálculos (por defecto velas de 8 horas como en el script MQL). |

## Flujo de trading

1. Suscribirse a velas definidas por `CandleType` e iniciar los indicadores DEMA.
2. Almacenar los valores de canal más recientes en colas para que el algoritmo pueda referenciar el valor que existía `Shift` barras antes, reproduciendo el desplazamiento del indicador original.
3. Cuando una vela finaliza, calcular su color de ruptura y añadirlo al buffer deslizante. Usar el buffer para identificar nuevas rupturas al alza o a la baja según `SignalBar`.
4. Cerrar posiciones existentes si aparece la señal opuesta o si el filtro de tiempo expira.
5. Entrar en nuevas operaciones enviando órdenes de mercado con tamaño `Volume + |Position|` para girar desde el lado opuesto cuando sea necesario.
6. Actualizar el timestamp interno de la posición activa para mantener el filtro de tiempo de mantenimiento preciso.

## Notas

- La estrategia asume que los datos del gráfico se procesan en orden cronológico. Al ejecutar en backtests o trading en vivo, asegúrese de que el flujo de velas esté ordenado para mantener el comportamiento de desplazamiento correcto.
- `Volume` debe establecerse en la estrategia antes del inicio (via UI o código) para controlar el dimensionamiento de posición. Los modos de gestión de dinero del experto MQL no están replicados intencionalmente.
- Debido a que las órdenes de protección son opcionales, recuerde configurar los valores de stop-loss y take-profit al desplegar en entornos de producción.
- El ayudante de gráficos dibuja velas y operaciones ejecutadas automáticamente, permitiendo verificación visual de que las rupturas del canal activan las entradas y salidas esperadas.
