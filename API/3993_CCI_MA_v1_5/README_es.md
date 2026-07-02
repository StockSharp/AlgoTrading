# CCI Estrategia MA v1.5
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia recrea el asesor experto MetaTrader "CCI_MA v1.5" dentro dla API de alto nivel de StockSharp. El robot original espera a que el índice del canal de productos básicos (CCI) cruce una media móvil simple calculada sobre los propios valores CCI y utiliza un CCI secundario para supervisar las salidas alrededor de los umbrales de ±100. El puerto StockSharp mantiene el mismo orden de señales, administración de dinero opcional y distancias de parada/objetivo basadas en puntos mientras adapta todo a las suscripciones de velas y vinculaciones de indicadores.

## como funciona
* **Fuente de datos**: una serie de velas definida por el usuario (velas de 15 minutos de forma predeterminada) alimenta ambas CCI. Los indicadores leen el precio de cierre de la vela para reflejar la configuración `PRICE_CLOSE` de MetaTrader.
* **Indicadores principales**: el `CommodityChannelIndex` principal (parámetro `CciPeriod`) proporciona la lectura del impulso. Se aplica un `SimpleMovingAverage` con período `MaPeriod` al flujo de valores CCI para formar la línea de activación. Un CCI (`SignalCciPeriod`) secundario supervisa las reversiones de sobrecompra y sobreventa alrededor de ±100.
* **Lógica de entrada**: se abre una operación larga en la barra después de un cruce hacia arriba: la vela completada anteriormente (`prevCci`) debe ubicarse por encima del promedio móvil CCI mientras que la vela anterior (`prev2Cci`) estaba por debajo. Una señal corta es el cruce simétrico hacia abajo. Las posiciones opuestas existentes se cierran y se invierten sumando el valor absoluto de la posición actual al nuevo tamaño de la orden, coincidiendo con el comportamiento de la versión MQL.
* **Lógica de salida**: las posiciones largas se liquidan cuando el CCI supervisor cae de más de +100 a menos de +100 o cuando el CCI primario vuelve a cruzar por debajo de su promedio móvil (evaluado nuevamente en las dos velas terminadas anteriormente). Los pantalones cortos salen en condiciones inversas. Las paradas de protección emulan las distancias basadas en puntos de MetaTrader: la estrategia deriva un tamaño de pip del instrumento `PriceStep` (multiplicando por 10 para cotizaciones de tres o cinco dígitos) y compara los extremos de las velas con `entry ± distance` en cada vela completa.
* **Tamaño de la posición**: `LotVolume` define el tamaño del pedido base. Si `UseMoneyManagement` está habilitado, la estrategia lo multiplica por un factor entero igual a `floor(balance / DepositPerLot)`, limitado por `MaxMultiplier`, reproduciendo la escalera de depósitos del asesor experto. El volumen del pedido está alineado con las restricciones del instrumento `VolumeStep`, `MinVolume` y `MaxVolume` antes del envío.

## Parámetros
- **Tipo de vela**: tipo de datos de vela que impulsa todos los cálculos del indicador.
- **CCI Período**: longitud del oscilador primario CCI.
- **Período de salida CCI**: duración del CCI de supervisión utilizado para las salidas de umbral.
- **CCI Período MA** – Período del promedio móvil simple aplicado al CCI primario.
- **Volumen de lote**: volumen de operaciones base antes de escalar la administración del dinero.
- **Habilitar administración de dinero**: activa el escalado del volumen del lote basado en depósitos.
- **Depósito por lote**: incremento de saldo necesario para aumentar el multiplicador del lote en uno (se usa solo cuando la administración de dinero está activa).
- **Multiplicador máximo**: multiplicador máximo que puede alcanzar la administración del dinero.
- **Stop Loss (pips)** – Distancia en pips para el tope de protección; póngalo en cero para desactivarlo.
- **Take Profit (pips)** – Distancia en pips para el objetivo de ganancias; póngalo en cero para desactivarlo.

La estrategia espera dos velas completamente cerradas antes de emitir la primera orden para que las comparaciones cruzadas de dos barras coincidan exactamente con la ejecución retrasada del experto MQL. Las comprobaciones de stop-loss y take-profit se ejecutan en velas terminadas utilizando sus extremos alto/bajo, lo que se aproxima a las órdenes de protección del lado del servidor de MetaTrader mientras se mantiene dentro del nivel alto StockSharp API.
