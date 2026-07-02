# Seguidor de tendencias de FT
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
FT Trend Follower es una StockSharp versión del MetaTrader 4 asesor experto `FT_TrendFollower.mq4`. La estrategia aprovecha las tendencias a mediano plazo al combinar un ventilador de media móvil múltiple Guppy (GMMA) con un disparador de oscilador de Laguerre, un cruce EMA rápido/lento y un filtro MACD. Las entradas sólo se activan después de que el mercado se sumerge en el paquete GMMA, se recupera de un extremo de Laguerre y la mayoría de las líneas GMMA reanudan su inclinación en la dirección del comercio. La gestión de ganancias refleja el EA original: una parada opcional basada en oscilación, una parada de distancia fija y tres módulos de salida por etapas mutuamente excluyentes impulsados ​​por niveles de pivote diarios o promedios de canal.

## Lógica estratégica
### Estructura GMMA y detección de tendencias.
* El ventilador GMMA se extiende desde `StartGmmaPeriod` hasta `EndGmmaPeriod`. Los períodos se distribuyen en cinco grupos de `BandsPerGroup` líneas cada uno, replicando la lógica `CountLine` original.
* La dirección de la tendencia compara el grupo GMMA más lento (índice `CountLine + CountLine` desde el final) con el grupo más rápido a largo plazo (índice `CountLine` desde el final). Los promedios crecientes a largo plazo definen una tendencia alcista; los que caen definen una tendencia bajista.
* La confirmación de pendiente cuenta cuántas líneas GMMA a corto, mediano y largo plazo aumentaron o disminuyeron en comparación con la barra anterior. Una operación requiere que el recuento de pendiente ascendente (o descendente) supere la mitad del total de líneas GMMA, imitando el umbral `controlvverh`/`controlvverhS` en MetaTrader.

### Cebado de señal
* **Cerrar reinicio** – Cuando la vela anterior se cierra por debajo de la línea GMMA más lenta, el módulo largo se arma; cuando se cierra por encima de la línea más lenta, el módulo corto se arma. Cruzar nuevamente por encima (o por debajo) del GMMA más rápido borra las banderas de armado, tal como la lógica `CloseOk` original.
* **Activador de Laguerre**: un filtro de Laguerre (`LaguerreGamma`) primero debe caer por debajo de `LaguerreOversold` (configuración larga) o subir por encima de `LaguerreOverbought` (configuración corta) mientras la vela aún respeta la GMMA a largo plazo. Sólo después de que el oscilador retrocede a través del umbral se puede disparar una entrada.
* **EMA cruce**: el EMA rápido (`FastSignalLength`) debe descender por debajo del EMA lenta (`SlowSignalLength`) para armar el módulo largo y luego cruzar nuevamente por encima de él para liberar la entrada. Los pantalones cortos revierten la desigualdad.
* Filtro **MACD**: la línea principal MACD (5/35/5 como en EA) debe ser positiva para posiciones largas y negativa para posiciones cortas.

### Reglas de entrada
Una operación larga se ejecuta cuando:
1. La detección de tendencias informa una tendencia alcista y la votación de la pendiente GMMA supera la mitad de las líneas disponibles.
2. El disparador de Laguerre se armó previamente y el valor actual se cierra nuevamente por encima de `LaguerreOversold`.
3. El EMA rápido está por encima del EMA lento después de haber estado anteriormente por debajo.
4. MACD es mayor que cero.

Las entradas cortas requieren condiciones simétricas con el oscilador cruzando por debajo de `LaguerreOverbought` y MACD negativo. Al revertir una posición existente, el tamaño de la orden compensa automáticamente la exposición anterior para que la posición neta final sea igual a `Volume`.

### Gestión de riesgos y salidas.
* **Paradas**: elija la parada de oscilación (`UseSwingStop`) ubicada debajo (sobre) de la vela anterior en `SwingStopPips` puntos, o la parada de distancia fija (`UseFixedStop`) compensada en `FixedStopPips` puntos. Si ambos están habilitados a la vez la estrategia aborta al inicio, reproduciéndose las reglas de validación EA.
* **Módulo de salida de pivote (Salir)**: cuando está habilitado, el primer cierre parcial (50 % de `Volume`) se activa una vez que el precio cruza el pivote R1/S1 del día anterior con ganancias no realizadas. El resto se cierra tan pronto como Hull MA produce un valor válido, que coincide con la verificación del búfer `hma1` de MetaTrader.
* **Módulo de salida de rango de pivote (Salir1)** – El cierre parcial inicial todavía ocurre en R1/S1. El resto sale en R2/S2 una vez que la operación sigue siendo rentable.
* **Módulo de salida de canal (Salir2)**: el primer cierre parcial ocurre en R1/S1. La estrategia cierra el resto cuando la vela se vuelve a abrir por debajo del canal bajo SMA (`ChannelPeriod`) para largos o por encima del canal alto SMA para cortos, reflejando el filtro de volatilidad original.

Solo puede haber un módulo de salida activo a la vez, al igual que la validación de parámetros de EA.

## Parámetros
* **Volumen**: tamaño del pedido para nuevas operaciones.
* **StartGmmaPeriod / EndGmmaPeriod** – Límites para los fanáticos de GMMA.
* **BandsPerGroup**: número de líneas GMMA muestreadas por grupo (CountLine en MT4).
* **FastSignalLength / SlowSignalLength** – EMA longitudes utilizadas para la confirmación de cruce.
* **TradeShift** – Se mantiene por compatibilidad; la implementación opera en velas terminadas, por lo que se rechazan los valores distintos de 0 o 1.
* **UseSwingStop / SwingStopPips**: habilita y configura la parada protectora basada en oscilación.
* **UseFixedStop/FixedStopPips**: habilita la parada de distancia fija medida en puntos de precio.
* **EnablePivotExit / EnablePivotRangeExit / EnableChannelExit**: módulos de salida por etapas mutuamente excluyentes.
* **LaguerreOversold / LaguerreOverbought / LaguerreGamma** – Umbrales de activación de Laguerre y factor de suavizado.
* **HmaPeriod** – Longitud MA del casco utilizada por el módulo de salida de pivote.
* **ChannelPeriod**: longitud del canal alto/bajo SMA para Quit2.
* **CandleType**: período de tiempo que impulsa los cálculos de la estrategia (predeterminado: velas de 1 hora).

## Notas adicionales
* Los niveles de pivote diarios se calculan a partir de la última vela diaria terminada proporcionada por una suscripción secundaria.
* Los puntos de precio y las conversiones de pips dependen del valor `PriceStep`. Los símbolos con diferentes tamaños de tick se adaptan automáticamente.
* La estrategia se suscribe únicamente a indicadores de alto nivel y evita lecturas directas del búfer, adhiriéndose a las pautas de alto nivel API del proyecto.
* No se proporciona ninguna implementación de Python en este paquete.
