# MA S.R. Estrategia comercial
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El MA S.R. La estrategia comercial es un sistema de inversión de tendencias convertido del asesor MetaTrader original "MA S.R Trading". Supervisa la forma de una media móvil simple corta (SMA) para detectar cuándo el impulso del precio se inclina hacia un máximo o un mínimo local. Cuando el SMA alcanza su punto máximo o mínimo, la estrategia entra inmediatamente en la dirección del giro y protege la posición con un nivel de stop anclado en el swing más reciente.

A diferencia de los sistemas cruzados clásicos que comparan múltiples promedios móviles con diferentes longitudes, este enfoque analiza la curvatura del mismo SMA comparando su valor en las tres velas completadas más recientes. Un máximo local (`SMA[t-2]` mayor que `SMA[t-1]` y `SMA[t-3]`) indica una reversión bajista y desencadena una entrada corta. Un mínimo local (`SMA[t-2]` por debajo de ambos vecinos) indica una reversión alcista y abre una posición larga. Inmediatamente después de una señal, la estrategia almacena el precio extremo en una ventana retrospectiva configurable y lo utiliza como stop de protección.

La lógica de salida imita la modificación final de la fuente MQL. Para operaciones cortas, el stop se establece en el máximo más alto dentro de la ventana retrospectiva, siempre que este nivel permanezca por encima del cierre anterior (de lo contrario, el nivel se ignora). Las posiciones largas utilizan el mínimo más bajo bajo la misma regla. Si el precio toca el nivel almacenado en las velas siguientes, la estrategia cierra la posición en el mercado, emulando efectivamente la actualización del stop-loss del experto original.

El sistema está diseñado para instrumentos que exhiben un comportamiento de oscilación pronunciado en gráficos intradiarios y de corto plazo. Los períodos cortos SMA (predeterminado = 5) permiten que el algoritmo reaccione rápidamente a los cambios de microestructura, mientras que el stop lookback (predeterminado = 5 barras tanto para máximos como para mínimos) controla la agresividad con la que el nivel final sigue al precio. Utilice ventanas más estrechas para entornos de especulación y entornos más amplios para mercados más ruidosos.

Las pruebas retrospectivas de las principales divisas y los CFD sobre índices líquidos muestran el mejor rendimiento durante períodos variables con oscilaciones frecuentes. Las tendencias con retrocesos suaves pueden requerir filtros adicionales o confirmación de volatilidad para evitar retrocesos prematuros. Considere combinar la estrategia con un contexto de mercado más amplio o filtros de tiempo cuando la implemente en vivo.

## Detalles

- **Condiciones de entrada**
  - **Breve**: `SMA[t-1] < SMA[t-2]` Y `SMA[t-3] < SMA[t-2]`. La última muestra SMA terminada forma un máximo local.
  - **Largo**: `SMA[t-1] > SMA[t-2]` Y `SMA[t-3] > SMA[t-2]`. La última muestra SMA terminada forma un mínimo local.
- **Detener gestión**
  - **Corto**: Nivel de parada = máximo más alto dentro de `HighLookback` velas si el nivel está por encima del cierre anterior. Sale cuando el precio toca el nivel.
  - **Largo**: Nivel de parada = mínimo más bajo dentro de `LowLookback` velas si el nivel está por debajo del cierre anterior. Sale cuando el precio toca el nivel.
- **Reglas de posición**: Siempre cambia a la última señal. Al revertir, la estrategia cierra la posición existente y abre la nueva en una única orden de mercado de tamaño para cubrir la exposición anterior más el volumen deseado.
- **Parámetros predeterminados**
  - `SmaPeriod` = 5.
  - `HighLookback` = 5.
  - `LowLookback` = 5.
  - `CandleType` = período de 30 minutos.
  - `TradeVolume` = 1 lote (aplicado a la propiedad `Volume` al inicio).
- **Filtros**
  - Categoría: Reversión.
  - Dirección: Tanto larga como corta.
  - Indicadores: media móvil simple, rastreador de swing más alto/más bajo.
  - Paradas: Dinámicas, basadas en swing.
  - Plazo: Intradiario para oscilar.
  - Complejidad: Media.
  - Nivel de riesgo: Moderado (stops estrictos pero operaciones frecuentes).

## Notas de uso

1. Funciona mejor en instrumentos con oscilaciones visibles. Considere desactivar el comercio en torno a eventos noticiosos importantes para evitar oscilaciones falsas.
2. Optimice el período SMA y las ventanas retrospectivas para el símbolo y el período de tiempo de destino. Los ajustes más pequeños aumentan la sensibilidad pero también las vibraciones.
3. Los niveles de parada se recalculan sólo cuando aparece una nueva señal de giro. Si un stop deja de ser válido (por ejemplo, un máximo no superior al cierre anterior), se descarta, evitando que la estrategia coloque niveles de protección demasiado cerca del precio.
4. Dado que las salidas dependen de las órdenes de mercado, pueden producirse deslizamientos en movimientos rápidos. Combine con órdenes de protección del corredor si el lugar las admite.
5. La estrategia no utiliza objetivos de obtención de beneficios. Para agregarlos, amplíe la lógica en `ProcessCandle` con condiciones adicionales.
