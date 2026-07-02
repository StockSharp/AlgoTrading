# Estrategia de cuadrícula de cesta Ilan 1.4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Ilan 1.4 es un sistema de cuadrícula de promedio clásico. La estrategia convertida se suscribe a una única serie de velas y abre una posición inicial de mercado basada en la dirección de las dos últimas velas completadas: si el cierre más reciente está por debajo del más antiguo, la cesta comienza con una venta; de lo contrario, abre una compra. Cuando el precio se mueve contra la cesta activa en el **Pip Step** configurado, la estrategia opcionalmente agrega una nueva posición en la misma dirección y recalcula el precio de entrada promedio ponderado.

Todas las operaciones dentro de la cesta se ejecutan con órdenes de mercado. Cuando el precio de cierre alcanza el precio medio de entrada más la distancia **Take Profit**, se cierra toda la cesta. Un trailing stop, un stop loss fijo, un stop de emergencia basado en acciones y una salvaguardia de vida útil máxima reproducen los bloques de protección del experto original MetaTrader.

## Reglas de trading
1. Espere la siguiente vela terminada y evalúe los dos últimos cierres.
2. Si no hay exposición, abra una cesta larga cuando el último cierre sea superior al anterior y una cesta corta en caso contrario.
3. Mantenga un registro del último precio de llenado y del precio de entrada promedio ponderado de la canasta activa.
4. Cuando **Usar Agregar** está habilitado y el precio se mueve contra la posición en puntos **Pip Step**, calcule el siguiente tamaño de lote y abra una operación de mercado adicional. Si **Cerrar antes de agregar** está habilitado, la cesta existente se cierra primero y se vuelve a abrir con el volumen escalado.
5. Vuelva a calcular el precio de entrada promedio después de cada llenado. La canasta se liquida una vez que el precio toca el nivel promedio de obtención de ganancias o cuando se activa alguna de las reglas de riesgo.
6. Una vez que se cierra una canasta, la lógica prepara inmediatamente una nueva señal utilizando los dos últimos cierres de velas.

## Modos de administración de dinero
El parámetro **Money Management** reproduce el modificador `MMType` original:
- **Solucionado**: cada nuevo pedido utiliza el **Volumen inicial** configurado.
- **Geométrico**: las órdenes posteriores multiplican el volumen base por `LotExponent^n`, donde `n` es igual al número actual de operaciones abiertas.
- **RecoverLastLoss** – después de una cesta perdedora, la siguiente posición utiliza el volumen de la última operación cerrada multiplicado por **Exponente de lote**; Las cestas rentables restablecen el volumen al valor base.

Los volúmenes comerciales se redondean según los **Dígitos de volumen** y el paso del volumen de seguridad. Cuando el redondeo produciría cero, se utiliza el volumen de entrada no redondeado.

## Controles de riesgo
- **Take Profit**: cierra toda la cesta una vez que el precio alcanza el precio de entrada promedio ± puntos configurados.
- **Stop Loss**: cierra la cesta cuando el precio se mueve contra el precio de entrada promedio en la cantidad especificada de puntos.
- **Usar Trailing Stop** con **Trail Start** y **Trail Stop**: activa un nivel de seguimiento una vez que la cesta gana suficientes puntos; la compensación final sigue el precio para proteger las ganancias.
- **Utilice Equity Stop** con **% de riesgo de capital**: monitorea el valor de la cartera y cierra la canasta cuando la pérdida flotante excede el porcentaje elegido del pico de capital registrado.
- **Usar tiempo de espera** con **Máximo de horas de apertura**: cierra con fuerza la cesta cuando permanece abierta más tiempo que el número de horas permitido.

## Parámetros
- **Tipo de vela**: período de tiempo utilizado para generar señales comerciales.
- **Volumen inicial**: tamaño del lote inicial para una canasta nueva.
- **Dígitos de volumen**: precisión utilizada al redondear los volúmenes calculados.
- **Gestión de dinero** – modo de cálculo de volumen (`Fixed`, `Geometric`, `RecoverLastLoss`).
- **Exponente de lote** – multiplicador aplicado por los esquemas geométrico y de recuperación.
- **Cerrar antes de agregar**: cierre todas las operaciones abiertas antes de realizar la siguiente orden promedio.
- **Usar Agregar**: habilita o deshabilita los pedidos promedio por completo.
- **Pip Step** – movimiento adverso mínimo (en pasos de precio) antes de agregar una nueva operación.
- **Take Profit**: objetivo de beneficio a partir del precio de entrada promedio.
- **Stop Loss**: desviación adversa máxima permitida del precio de entrada promedio.
- **Usar Trailing Stop / Trail Start / Trail Stop** – configuración trailing-stop.
- **Max Trades**: número máximo de operaciones promedio permitidas dentro de una cesta.
- **Utilice Equity Stop / Equity Risk %**: parámetros de la protección contra pérdidas flotantes.
- **Usar tiempo de espera / Horas máximas de apertura**: control de vida útil de cada canasta.

## Notas de conversión
- MetaTrader ayudantes de órdenes pendientes fueron reemplazados por órdenes de mercado directas porque la lógica de promedio siempre se ejecutaba inmediatamente en el código original.
- El bloque final ahora funciona en la cesta agregada en lugar de modificar cada pedido por separado; Las distancias de disparo coinciden con los valores predeterminados originales.
- El valor de la cartera se monitorea a través del objeto de cartera StockSharp para emular la rutina de parada de acciones del experto.
- Los promedios de posición y las estadísticas de la cesta se calculan dentro de la estrategia sin almacenar colecciones por operación, respetando las pautas de alto nivel API.
