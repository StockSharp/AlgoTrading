# Estrategia Exp i-KlPrice Vol
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción General
Esta estrategia es una conversión en C# del experto de MetaTrader **Exp_i-KlPrice_Vol.mq5**. Reconstruye el oscilador KlPrice
que mide la distancia entre el precio y una banda de volatilidad, multiplica el oscilador por el volumen de la vela y rastrea
las transiciones de color generadas por umbrales adaptativos. Se emulan dos ranuras de posición independientes para cada
dirección, reflejando el comportamiento de doble magic del asesor experto original.

## Lógica del Indicador
- El precio se transforma usando el modo `AppliedPrice` seleccionado (close, open, median, Demark, etc.).
- El precio transformado se suaviza por el método de media móvil definido en `PriceMaMethod` y `PriceMaLength`.
- El rango de la vela (`High - Low`) se suaviza con `RangeMaMethod`/`RangeMaLength`. El rango actúa como ancho dinámico de banda.
- El oscilador KlPrice base se calcula como `100 * (Price - (MA - RangeMA)) / (2 * RangeMA) - 50`.
- El oscilador se multiplica por la fuente de volumen seleccionada (`AppliedVolume.Tick` o `AppliedVolume.Real`).
- Se aplica un suavizador Jurik de longitud `SmoothingLength` tanto al oscilador como al volumen bruto, creando dos series
  adaptativas.
- Los umbrales adaptativos se obtienen multiplicando el volumen suavizado por `HighLevel2`, `HighLevel1`, `LowLevel1` y `LowLevel2`.
- El color actual del oscilador se determina comparando el valor del oscilador suavizado con los umbrales adaptativos:
  - **4** – por encima de `HighLevel2 * volume` (presión alcista extrema).
  - **3** – entre `HighLevel1 * volume` y el nivel extremo.
  - **2** – entre los umbrales alcista y bajista.
  - **1** – entre el umbral inferior y la línea neutral.
  - **0** – por debajo de `LowLevel2 * volume` (presión bajista extrema).

## Reglas de Trading
1. Evaluar el color en `SignalBar` (generalmente la vela completada anterior) y el color antes de él.
2. Entradas largas:
   - La Ranura 1 se abre cuando el color cambia de **4** a cualquier valor por debajo de **4** y `AllowLongEntry` es `true`.
   - La Ranura 2 se abre cuando el color cambia de **3** a por debajo de **3**.
3. Entradas cortas:
   - La Ranura 1 se abre cuando el color sube de **0** a por encima de **0** y `AllowShortEntry` es `true`.
   - La Ranura 2 se abre cuando el color sube de **1** a por encima de **1**.
4. Las salidas largas ocurren cuando el color anterior era **0** o **1** y `AllowLongExit` está habilitado.
5. Las salidas cortas ocurren cuando el color anterior era **4** o **3** y `AllowShortExit` está habilitado.
6. Cada ranura rastrea el tiempo del último señal para evitar órdenes duplicadas en la misma vela. Los stops de protección son
   opcionales y se manejan a través de `StartProtection` cuando `StopLossPoints` o `TakeProfitPoints` son mayores que cero.

## Parámetros
| Nombre | Tipo | Valor predeterminado | Descripción |
|--------|------|----------------------|-------------|
| `PrimaryVolume` | `decimal` | `0.1` | Volumen usado por la primera ranura largo/corto. |
| `SecondaryVolume` | `decimal` | `0.2` | Volumen usado por la segunda ranura. |
| `StopLossPoints` | `int` | `1000` | Distancia de stop de protección opcional en pasos de precio. |
| `TakeProfitPoints` | `int` | `2000` | Distancia de take-profit opcional en pasos de precio. |
| `AllowLongEntry` | `bool` | `true` | Habilitar apertura de posiciones largas. |
| `AllowShortEntry` | `bool` | `true` | Habilitar apertura de posiciones cortas. |
| `AllowLongExit` | `bool` | `true` | Cerrar posiciones largas cuando aparecen colores bajistas. |
| `AllowShortExit` | `bool` | `true` | Cerrar posiciones cortas cuando aparecen colores alcistas. |
| `CandleType` | `DataType` | `H8` | Marco temporal de vela para cálculos. |
| `PriceMaMethod` | `SmoothMethod` | `Sma` | Tipo de media móvil usada en el precio aplicado. |
| `PriceMaLength` | `int` | `100` | Longitud del suavizador de precio. |
| `PriceMaPhase` | `int` | `15` | Parámetro de fase para filtros basados en Jurik. |
| `RangeMaMethod` | `SmoothMethod` | `Jjma` | Tipo de media móvil usada en el rango de la vela. |
| `RangeMaLength` | `int` | `20` | Longitud del suavizador de rango. |
| `RangeMaPhase` | `int` | `100` | Parámetro de fase para el suavizador de rango. |
| `SmoothingLength` | `int` | `20` | Longitud de suavizado Jurik aplicada al oscilador y volumen. |
| `AppliedPrice` | `AppliedPrice` | `Close` | Fuente de precio usada en cálculos del oscilador. |
| `VolumeType` | `AppliedVolume` | `Tick` | Fuente de volumen multiplicada por el oscilador. |
| `HighLevel2` | `int` | `150` | Multiplicador extremo superior para el umbral adaptativo. |
| `HighLevel1` | `int` | `20` | Multiplicador moderado superior. |
| `LowLevel1` | `int` | `-20` | Multiplicador moderado inferior. |
| `LowLevel2` | `int` | `-150` | Multiplicador extremo inferior. |
| `SignalBar` | `int` | `1` | Desplazamiento histórico usado para leer transiciones de color. |

## Notas de Uso
- Conecte la estrategia a un instrumento que proporcione información de precio y volumen; cuando solo está disponible el volumen
  de ticks, se usa el contador de ticks como proxy.
- Los dos volúmenes de ranura pueden ajustarse independientemente para emular la configuración de gestión de dinero dual del EA
  original.
- Ajuste `SignalBar` cuando trabaje con velas parcialmente formadas o cuando resincronice datos históricos.
- Los métodos de suavizado admiten filtros Jurik a través de reflexión para replicar el comportamiento de la biblioteca MQL
  `SmoothAlgorithms`.
- Dado que `StartProtection` se invoca solo cuando las distancias de stop o target son positivas, déjelas en cero para
  deshabilitar las órdenes de protección.
