# Conejo M3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Rabbit M3 es una adaptación del MetaTrader 4 asesor experto `RabbitM3` (también lanzado con el nombre "Petes Party Trick"). El sistema alterna entre regímenes de sólo largo y sólo de corto utilizando un par de medias móviles exponenciales horarias. La confirmación del impulso proviene de un cruce Williams %R combinado con un filtro de nivel CCI, mientras que un canal Donchian extremadamente largo busca desgloses de precios que invaliden el sesgo de tendencia actual. Opcionalmente, el tamaño de la posición puede crecer después de los grandes ganadores, replicando la regla de escala de lotes contenida en el código original.

## Lógica estratégica
### Filtro de régimen de tendencias
* Cuando el EMA rápido cierra por debajo del EMA lento, cualquier exposición larga existente se liquida y las nuevas señales se restringen al lado corto.
* Cuando el EMA rápido cierra por encima del EMA lento, cualquier exposición corta existente se cierra y solo las configuraciones largas siguen siendo elegibles.
* Si las EMA son iguales, se mantiene el régimen anterior, reflejando la lógica MetaTrader que solo alterna desigualdades estrictas.

### Reglas de entrada
* **Operaciones cortas**
  * El régimen debe ser solo corto (EMA rápida debajo de EMA lenta).
  * Williams %R (longitud = `WilliamsPeriod`) debe cruzar hacia abajo a través del `WilliamsSellLevel` en la vela más reciente mientras el valor anterior aún estaba por debajo de cero.
  * CCI (longitud = `CciPeriod`) debe ser mayor o igual a `CciSellLevel`.
  * La posición neta debe ser plana; la estrategia abre como máximo `MaxOpenPositions` operaciones y de forma predeterminada utiliza una única orden de mercado de tamaño `EntryVolume`.
* **Operaciones largas**
  * El régimen debe ser solo largo (EMA rápida arriba de EMA lenta).
  * Williams %R debe cruzar `WilliamsBuyLevel` mientras el valor anterior todavía estaba por debajo de cero.
  * CCI debe ser menor o igual a `CciBuyLevel`.
  * La posición neta debe ser plana antes de que se inicie una nueva posición larga.

### reglas de salida
* **Paradas bruscas**: `StopLossPips` y `TakeProfitPips` se convierten en compensaciones de precios utilizando el paso de precio del instrumento. Un valor de `0` desactiva el nivel de protección correspondiente.
* **Donchian ruptura**: si el precio cierra por encima de la banda superior Donchian anterior (longitud = `DonchianLength`), cualquier posición corta se cierra inmediatamente. Un cierre por debajo de la banda inferior anterior cierra posiciones largas. El canal utiliza el valor completado anteriormente para reproducir el retraso `iHighest`/`iLowest` del EA.
* **Cambio de régimen**: cada vez que la relación EMA se invierte, la estrategia liquida la exposición contraria antes de permitir nuevas operaciones en la nueva dirección.

### gestión del dinero
* Comienza con `EntryVolume` unidades por operación.
* Cuando se produce una ganancia realizada mayor que `BigWinThreshold` mientras la estrategia es plana, el volumen aumenta en `VolumeIncrement` y el umbral se duplica (4 → 8 → 16, etc.). Si cualquiera de los parámetros se establece en `0`, la regla de escala está deshabilitada.

## Parámetros
* **Período EMA rápida**: duración del filtro de tendencia rápida (predeterminado: 33).
* **Período EMA lenta**: duración del filtro de tendencia lenta (predeterminado: 70).
* **Williams %R Period**: búsqueda retrospectiva del oscilador Williams %R (predeterminado: 62).
* **Williams Nivel de venta**: límite superior que debe cruzarse hacia abajo para señales cortas (predeterminado: −20).
* **Williams Nivel de compra**: límite inferior que debe cruzarse hacia arriba para señales largas (predeterminado: −80).
* **CCI Período**: retrospectiva del índice de canales de productos básicos (predeterminado: 26).
* **CCI Nivel de venta**: valor mínimo de CCI requerido para permitir posiciones cortas (predeterminado: 101).
* **CCI Nivel de compra**: valor máximo de CCI requerido para permitir posiciones largas (predeterminado: 99).
* **Donchian Longitud**: número de velas completadas muestreadas para la salida de ruptura (predeterminado: 410).
* **Posiciones abiertas máximas**: operaciones simultáneas máximas; la configuración clásica utiliza un contrato (predeterminado: 1).
* **Take Profit (pips)**: objetivo de beneficio medido en pasos de precio (predeterminado: 360).
* **Stop Loss (pips)** – stop de protección medido en pasos de precio (predeterminado: 20).
* **Volumen de entrada**: tamaño inicial del pedido (predeterminado: 0,01).
* **Umbral de grandes ganancias**: se requieren ganancias obtenidas antes de aumentar el tamaño (predeterminado: 4.0).
* **Incremento de volumen**: volumen adicional agregado después de superar el umbral (predeterminado: 0,01).
* **Tipo de vela**: período de tiempo utilizado para todos los cálculos del indicador (predeterminado: velas por hora).

## Notas adicionales
* La conversión de pips se basa en el valor `PriceStep`. Los instrumentos sin un escalón de precio vuelven a tener un valor de pip unitario.
* Los niveles de Donchian están retrasados intencionalmente una vela, por lo que la salida refleja la lógica `shift=1` de las llamadas MetaTrader originales.
* El escalado de volumen solo evalúa el PnL realizado mientras la posición es plana, lo que evita que las ganancias flotantes desencadenen falsos positivos.
* Los objetos de etiqueta de la interfaz de usuario presentes en la fuente EA se omiten porque StockSharp visualiza el estado a través de gráficos y registros.
* En este paquete sólo se proporciona la implementación de C#; no existe una versión de Python.
