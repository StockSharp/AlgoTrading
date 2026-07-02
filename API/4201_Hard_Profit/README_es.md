# Beneficio duro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Hard Profit es una versión StockSharp del MetaTrader 4 asesores expertos `hardprofit.mq4`. La estrategia intenta capturar rupturas.
después de un movimiento de agotamiento cuando el cierre termina en el extremo de la vela y un filtro de tendencia suavizado confirma la dirección.
El puerto reconstruye los modos originales de administración de dinero, la toma de ganancias por etapas y la administración de paradas mediante el uso de StockSharp
API de alto nivel.

## Lógica estratégica
### Configuración de ruptura
* La estrategia monitorea las velas terminadas desde el período de tiempo configurado y realiza un seguimiento del máximo más alto y el mínimo más bajo del
barras `Breakout Period` anteriores (la vela actual se excluye, emulando la llamada `iHighest`/`iLowest` con un desplazamiento de 1).
* Los precios medianos alimentan un promedio móvil suavizado con el período `Trend Period`. La pendiente de la media móvil (valor actual menos
valor anterior) es el filtro direccional utilizado por el EA original.

### Reglas de entrada
* **Las entradas largas** se consideran cuando:
  * La vela cierra en su máximo y supera el máximo del rango anterior.
  * La pendiente media móvil suavizada es positiva.
  * No hay ninguna posición abierta y no se ha alcanzado el límite de operaciones por barra.
  * El diferencial actual (mejor demanda menos mejor oferta) está por debajo del umbral `Max Spread (pips)` cuando ambas partes están disponibles.
  * Las operaciones largas no están deshabilitadas por `Only Short`.
* **Entradas cortas** reflejan las condiciones anteriores: cierre en el mínimo, ruptura por debajo del mínimo del rango anterior, pendiente de tendencia negativa,
filtro de propagación respetado y `Only Long` deshabilitado.

### Gestión de salidas
* Un stop-loss fijo (`Stop Loss (pips)`) y una toma de ganancias opcional (`Take Profit (pips)`) definen la envoltura protectora exterior.
* Cuando el beneficio no realizado alcanza `Break-even (pips)`, el tope se traslada al precio de entrada. Después de `Trailing Activation (pips)` el
stop salta hacia adelante por la distancia de stop-loss, asegurando ganancias al igual que la implementación MetaTrader.
* Dos salidas parciales reciclan los porcentajes originales:
  * `Partial TP1 (pips)` cierra `Partial Ratio 1 (%)` de la posición activa.
  * `Partial TP2 (pips)` cierra `Partial Ratio 2 (%)` de la posición restante.
La lógica funciona en el volumen de la posición actual, por lo que el segundo parcial escala con lo que quede después del primer recorte.
* Los stop y los objetivos reaccionan a los extremos intrabar: una operación larga saldrá cuando el mínimo de la vela supere el stop o cuando el máximo
toca el objetivo de ganancias; Las operaciones cortas utilizan las condiciones simétricas.

### gestión del dinero
Cinco modos de tamaño imitan el comportamiento de MetaTrader y al mismo tiempo tienen en cuenta los datos de la cartera de StockSharp:
1. **Corregido**: utiliza `Fixed Volume` en cada entrada.
2. **Geométrico**: escala con la raíz cuadrada del valor de la cartera (`0.1 * sqrt(balance / 1000) * Geometrical Factor`).
3. **Proporcional**: asigna una fracción del capital libre en relación con el último cierre (`equity * Risk Percent / (price * 1000)`).
4. **Inteligente**: comienza desde la asignación proporcional y reduce el tamaño cuando se detecta más de una pérdida consecutiva por
usando el divisor `Decrease Factor`.
5. **TSSF**: recrea la lógica del factor de seguridad inteligente activado. La ganancia promedio, la pérdida promedio y la tasa de ganancia se calculan a partir de la mayoría
resultados recientes obtenidos de `Last Trades`. La métrica derivada cambia entre los divisores `TSSF Ratio` configurados o retrocede
a un mínimo de 0,1 lote cuando las condiciones se deterioran. Todos los volúmenes están normalizados al `VolumeStep`, `MinVolume`,
y restricciones `MaxVolume`.

## Parámetros
* **Período de ruptura**: número de velas terminadas utilizadas para calcular los máximos y mínimos de ruptura.
* **Período de tendencia**: duración de la media móvil suavizada aplicada al precio medio.
* **Solo corto / Solo largo**: alterna direccionales que desactivan el lado opuesto.
* **Max Trades Per Bar** – guardia de comercio por barra (0 desactiva el límite).
* **Stop Loss (pips)** – distancia inicial del stop-loss; configúrelo en 0 para desactivarlo.
* ** Punto de equilibrio (pips) **: umbral de beneficio que mueve el stop al nivel de entrada.
* **Activación de seguimiento (pips)**: umbral de beneficio que mueve el stop hacia adelante según el tamaño del stop original.
* **TP1 parcial (pips)** / **Ratio parcial 1 (%)** – distancia y porcentaje para la primera salida parcial.
* **TP2 parcial (pips)** / **Ratio parcial 2 (%)** – distancia y porcentaje para la segunda salida parcial.
* **Take Profit (pips)** – objetivo de beneficio final; 0 desactiva el objetivo difícil.
* **Max Spread (pips)** – spread máximo permitido en el momento de la entrada.
* **Administración de dinero**: selecciona el modo de tamaño (Fijo, Geométrico, Proporcional, Inteligente, TSSF).
* **Volumen fijo**: volumen base cuando el modo de administración de dinero es Fijo.
* **Factor geométrico** – multiplicador utilizado por la fórmula de tamaño geométrico.
* **Porcentaje de riesgo**: porcentaje del capital libre utilizado por el dimensionamiento proporcional, inteligente y TSSF.
* **Últimas operaciones**: número de operaciones realizadas recientemente almacenadas para un tamaño adaptable.
* **Factor de disminución**: divisor aplicado al modo inteligente cuando ocurren pérdidas consecutivas.
* **TSSF Trigger 1/2/3 y TSSF Ratio 1/2/3**: umbrales y divisores para las transiciones métricas de TSSF.
* **Tipo de vela**: período de tiempo principal que impulsa las actualizaciones de indicadores y la evaluación de señales.

## Notas adicionales
* Los valores de los pips se derivan del paso del precio del valor; Los símbolos FX de cinco dígitos asignan automáticamente un pip a 10 puntos.
* Las salidas parciales no restablecen el contador de transacciones por barra, replicando el comportamiento MetaTrader de contar solo nuevas entradas.
* Las estadísticas de gestión del dinero se crean a partir de las diferencias PnL observadas, por lo que el historial adquiere significado una vez que se realizan las primeras operaciones.
cerrar en el entorno StockSharp.
* Si los mejores datos de oferta/demanda no están disponibles, el filtro de diferencial se desactiva efectivamente, coincidiendo con el comportamiento del EA original cuando
el corredor informó un diferencial cero.
