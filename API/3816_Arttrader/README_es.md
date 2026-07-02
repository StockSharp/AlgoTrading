# Estrategia ArtTrader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Arttrader es una conversión del MetaTrader 4 asesores expertos `Arttrader_v1_5`. El sistema opera con velas horarias e intenta capturar movimientos direccionales suaves medidos por un promedio móvil exponencial (EMA) del precio de apertura. Las entradas se filtran tanto por la pendiente EMA como por una estricta verificación de la posición del precio dentro de la barra, mientras que un guardia de volatilidad dedicado bloquea las operaciones después de grandes brechas de apertura. Las posiciones se gestionan mediante un procedimiento de parada de pérdidas cronometrado, niveles fijos de parada de emergencia y toma de ganancias, y un mecanismo de seguridad basado en el volumen.

El puerto StockSharp mantiene las entradas originales y ejecuta operaciones a través de órdenes de mercado de alto nivel. Todos los cálculos se realizan sobre velas terminadas; Los requisitos de tiempo intrabar del asesor experto se aproximan comparando los retrasos de minutos configurados con la duración de la vela.

## Lógica estratégica
### Indicador
* **Precio de apertura EMA**: un único EMA con período configurable (`EMA Speed`) se calcula sobre el precio de apertura de la vela. La diferencia entre los valores EMA actuales y anteriores define la pendiente en pips.

### Filtros
* **Límites de pendiente**: la pendiente EMA debe estar entre los umbrales mínimo (`Slope Min`) y máximo (`Slope Max`). La estrategia ignora las operaciones cuando la tendencia es demasiado débil o demasiado fuerte.
* **Alineación intrabarra**: las operaciones largas requieren que la vela cierre por debajo o igual a su apertura y permanezca dentro del mínimo más el deslizamiento de entrada configurado. Las operaciones cortas reflejan la condición en torno al máximo. Los parámetros de retardo (`Entry Delay`, `Exit Delay`) se cumplen cuando la duración de la vela actual es al menos tan larga como los minutos configurados.
* **Guardia contra picos de volatilidad**: evalúa las diferencias de apertura a apertura en las últimas cinco velas. Si cualquier brecha simple excede `Big Jump` pips, o cualquier brecha de dos barras excede `Double Jump` pips, se bloquean nuevas entradas para la barra actual.

### Entradas
* **Entrada larga**: se activa cuando pasan todos los filtros, la pendiente EMA es positiva y no existe ninguna posición. El precio de entrada sintético almacenado se ajusta mediante el parámetro `Spread Adjust` para emular la compensación del diferencial original.
* **Entrada corta**: lógica simétrica que requiere una pendiente EMA negativa y ninguna posición activa.

### Salidas
* **Parada inteligente programada**: una vez en ganancias o pérdidas, la estrategia evalúa la parada inteligente solo después de que se cumple el requisito `Exit Delay`. Para posiciones largas, exige que el cierre esté por encima de la apertura y lo suficientemente cerca del máximo, mientras que la pérdida en pips en relación con el precio de entrada sintético debe exceder `Smart Stop`.
* **Volumen a prueba de fallos**: si el volumen de vela completado previamente es menor o igual a `Min Volume`, cualquier posición abierta se cierra inmediatamente en la siguiente barra.
* **Parada de emergencia/toma de ganancias**: tan pronto como se abre una operación, se registra una parada de emergencia brusca y un nivel fijo de toma de ganancias. Si el rango de velas alcanza cualquiera de los niveles, la posición se cierra sin esperar los filtros cronometrados.

## Parámetros
* **Volumen de la orden**: tamaño de la operación utilizada para las órdenes de mercado.
* **EMA Período**: duración del EMA aplicado a las aperturas de velas.
* **Big Jump (pips)**: espacio de apertura de barra única más grande permitido antes de que se supriman las señales de entrada.
* **Salto doble (pips)**: espacio de apertura de dos barras más grande permitido antes de que se supriman las señales de entrada.
* **Smart Stop (pips)**: distancia de pips necesaria para activar la lógica de stop-loss temporizada.
* **Parada de emergencia (pips)**: distancia de parada brusca evaluada en cada máximo/mínimo de vela.
* **Take Profit (pips)** – distancia fija de obtención de beneficios evaluada en cada máximo/mínimo de cada vela.
* **Pendiente mínima/pendiente máxima (pips)** – EMA límites de pendiente para la elegibilidad comercial.
* **Retraso de entrada (min)**: duración mínima de la vela (en minutos) antes de que se permitan las entradas.
* **Retraso de salida (min)**: duración mínima de la vela (en minutos) antes de que se pueda ejecutar la parada programada.
* **Deslizamiento de entrada / Deslizamiento de salida (pips)** – tolerancia entre el cierre y el extremo al validar filtros de entrada y salida.
* **Volumen mínimo** – volumen mínimo de vela anterior; las operaciones se cierran si no se excede el valor.
* **Ajuste del diferencial (pips)**: compensación del diferencial sintético aplicada al precio de entrada almacenado.
* **Deslizamiento (pips)**: configuración informativa conservada para compatibilidad con las entradas MetaTrader.
* **Tipo de vela**: período de tiempo utilizado para las suscripciones de velas (el valor predeterminado es velas de 1 hora).

## Notas
* La implementación StockSharp ejecuta órdenes de mercado y borra posiciones usando `BuyMarket`/`SellMarket`, coincidiendo con el comportamiento de posición única del EA original.
* Debido a que StockSharp opera en velas terminadas, las comprobaciones de minutos intrabar de MetaTrader se aproximan comparando los retrasos configurados con la duración total de la vela.
* Los niveles de parada de emergencia y toma de ganancias se evalúan contra los máximos y mínimos de las velas, emulando las órdenes de protección del corredor de la versión MetaTrader.
