# Estrategia de Rompimiento The 20s
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es una conversión en C# del asesor experto de MetaTrader **Exp_The_20s_v020**. Reproduce la lógica del indicador original "The 20s" que busca patrones de rompimiento después de una compresión de volatilidad. El algoritmo analiza velas completadas de un marco temporal configurable y reacciona cuando el precio rompe las bandas del 20% alrededor del rango de la barra anterior. La implementación mantiene el carácter de alto nivel de la API de StockSharp y expone todos los permisos de trading para que puedas habilitar o deshabilitar acciones largas o cortas de forma independiente.

## Lógica de señal
El indicador monitorea las velas más recientes y calcula niveles de referencia a partir de la barra anterior:

1. Medir el rango de la vela anterior: `range = high[1] - low[1]`.
2. Construir dos umbrales alrededor de esa barra:
   - `top = high[1] - range * Ratio`
   - `bottom = low[1] + range * Ratio`
3. Comparar la vela actual con los umbrales y la distancia `LevelPoints` (convertida a precio usando el `PriceStep` del instrumento).

El código original expone dos modos de cálculo:

- **Mode1 (predeterminado)** – busca un falso rompimiento dentro de la banda del 20% en la vela anterior seguido de un rechazo fuerte en la vela actual. Dependiendo de `IsDirect`, la estrategia compra la caída (`Direct = true`) o la vende (`Direct = false`).
- **Mode2** – requiere una serie de tres velas en expansión antes de la señal. Si la compresión estalla a la baja y el precio abre por debajo de la banda inferior, se activa una dirección; si abre por encima de la banda superior, se activa la dirección opuesta. `IsDirect` nuevamente invierte la dirección para coincidir con el comportamiento del EA original.

El parámetro `SignalBar` pospone la ejecución varios barras (0 = vela actual, 1 = vela anterior, etc.). Esto reproduce la capacidad del asesor experto de actuar sobre señales más antiguas una vez que están completamente formadas.

## Gestión de operaciones
- **Entradas**: `AllowLongEntry` y `AllowShortEntry` controlan si se abren nuevas posiciones. El parámetro `OrderVolume` define el tamaño de operación para cualquier posición nueva.
- **Reversiones de posición**: Cuando aparece una señal alcista, la estrategia primero cubre cualquier exposición corta (`AllowShortExit`) y luego opcionalmente abre una posición larga. La señal bajista refleja esta lógica para las posiciones largas.
- **Stops y objetivos**: `StopLossPoints` y `TakeProfitPoints` se miden en puntos del instrumento. Se convierten a precios usando `PriceStep` y se evalúan en cada vela completada. Si se toca algún nivel, la posición se cierra inmediatamente.
- **Modo directo**: Establecer `IsDirect` en `true` imita las salidas del indicador original. Cambiarlo a `false` invierte las direcciones de las flechas, lo cual es útil cuando quieres reflejar el comportamiento en mercados con diferentes características.

## Parámetros
- `OrderVolume` – predeterminado `1`. Tamaño del lote para nuevas posiciones.
- `StopLossPoints` – predeterminado `1000`. Stop protector en puntos (`0` lo deshabilita).
- `TakeProfitPoints` – predeterminado `2000`. Objetivo de ganancia en puntos (`0` lo deshabilita).
- `AllowLongEntry` / `AllowShortEntry` – habilitar entradas largas/cortas.
- `AllowLongExit` / `AllowShortExit` – permitir a la estrategia cerrar posiciones existentes cuando ocurran señales opuestas.
- `SignalBar` – predeterminado `1`. Número de barras a esperar antes de actuar sobre una señal.
- `LevelPoints` – predeterminado `100`. Distancia que confirma rompimientos más allá de los extremos de la barra anterior.
- `Ratio` – predeterminado `0.2`. Ancho de las bandas del 20% alrededor de la vela anterior.
- `IsDirect` – predeterminado `false`. Mantiene el mapeo original de compra/venta cuando es `true`, lo invierte cuando es `false`.
- `Mode` – predeterminado `Mode1`. Selecciona entre los dos algoritmos de cálculo.
- `CandleType` – predeterminado marco temporal `H1`. Define la suscripción usada para los cálculos.

## Notas
- La estrategia trabaja solo con velas completadas; las velas parciales se ignoran para evitar operaciones prematuras.
- Todas las entradas de registro y comentarios en línea están en inglés para mantener el código consistente con las muestras de StockSharp.
- La gestión del stop y objetivo se maneja dentro de la estrategia y no depende de órdenes adicionales, lo que hace que el comportamiento sea portable entre simuladores y brokers en vivo.
- Puedes adjuntar la estrategia a cualquier instrumento. Solo asegúrate de que la propiedad `PriceStep` esté disponible para que las distancias basadas en puntos se conviertan correctamente.
- Considera combinar `Mode2` con un `SignalBar` más grande en marcos temporales más altos para emular el comportamiento de "esperar confirmación" del EA.
