# Estrategia ColorJFatl Digit NN3 MMRec (Conversión StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia es un port de alto nivel de StockSharp del experto de MetaTrader 5 *Exp_ColorJFatl_Digit_NN3_MMRec*. El robot original usaba un indicador ColorJFatl_Digit personalizado junto con reglas de recuperación de gestión monetaria. La versión de StockSharp se enfoca en el motor de señales central y lo expresa a través de tres módulos independientes que trabajan en diferentes marcos temporales.

Cada módulo aplica una Media Móvil Jurik (JMA) a la fuente de precio seleccionada y monitorea la pendiente de ese promedio. Cuando la pendiente se vuelve positiva, el módulo lo trata como un régimen alcista, cierra la exposición corta y opcionalmente abre una nueva posición larga. Cuando la pendiente se vuelve negativa, el módulo realiza la lógica espejo para operaciones cortas. Todos los módulos comparten el mismo portafolio y por lo tanto siempre trabajan con la posición neta de la estrategia.

## Lógica de trading

1. Suscribirse a velas en tres marcos temporales (por defecto: 1 día, 8 horas, 3 horas).
2. Para cada vela completada:
   - Convertir la vela al precio aplicado configurado (cierre, apertura, precio típico, precio DeMark, etc.).
   - Procesar el valor a través de una Media Móvil Jurik para obtener una serie suavizada.
   - Comparar el valor JMA actual con el anterior para determinar la dirección de la pendiente. Una pendiente positiva produce un estado "arriba", una pendiente negativa produce un estado "abajo", una pendiente plana mantiene el estado anterior.
   - Almacenar en buffer los estados según el retraso *SignalBar* para que la estrategia pueda actuar en barras históricas si se desea (el experto original admitía señales retrasadas).
3. Cada vez que un módulo detecta una transición:
   - **Transición hacia arriba**: opcionalmente cerrar cualquier posición corta y abrir una posición larga con el volumen del módulo.
   - **Transición hacia abajo**: opcionalmente cerrar cualquier posición larga y abrir una posición corta con el volumen del módulo.
4. Las señales opuestas de otro módulo pueden liquidar o revertir la posición dependiendo de las banderas de habilitación.

Los stops y beneficios no están codificados; en cambio, la estrategia se basa en señales opuestas y las protecciones integradas de StockSharp (`StartProtection()`) para la seguridad.

## Parámetros

Los parámetros se agrupan por módulo (A, B, C) para reflejar la plantilla MT5. Cada grupo expone los siguientes valores:

- **CandleType** – marco temporal para las velas entrantes.
- **JmaLength** – período de la Media Móvil Jurik.
- **JmaPhase** – almacenado para documentación; el JMA de StockSharp no expone ajuste de fase.
- **SignalBar** – número de barras completadas a esperar antes de actuar en una señal (0 = inmediato).
- **AppliedPrices** – transformación de precio usada como entrada para JMA (cierre, apertura, mediana, típico, ponderado, simple, cuarto, seguimiento de tendencia, DeMark).
- **AllowBuyOpen / AllowSellOpen** – permiso para abrir posiciones en la dirección correspondiente.
- **AllowBuyClose / AllowSellClose** – permiso para cerrar posiciones existentes cuando el módulo emite una señal opuesta.
- **Volume** – tamaño de la orden que el módulo usa al abrir una nueva operación.

Como los módulos comparten una única posición de cuenta, solo puede existir una posición neta larga o neta corta a la vez. Si un módulo intenta abrir una operación mientras el portafolio ya tiene exposición en la misma dirección, la orden se omite; si hay una dirección opuesta abierta, se cierra antes de colocar la nueva operación (sujeto a las banderas de permiso).

## Notas de uso

- La estrategia suscribe todos los marcos temporales configurados a través de `GetWorkingSecurities()`, asegurando que el entorno de simulación o en vivo cargue las series de velas requeridas.
- Las señales operan estrictamente en velas completadas para prevenir el redibujado intra-barra.
- El enum *AppliedPrices* reproduce las opciones del indicador original, incluyendo dos variantes de precio de seguimiento de tendencia y el precio DeMark.
- La lógica de recuperación de gestión monetaria de la versión MQL no está reproducida. En cambio, el riesgo se puede gestionar mediante las protecciones de StockSharp o ajustando los volúmenes de los módulos.
- Los comentarios en inglés dentro del código explican cada paso de la conversión para facilitar el mantenimiento y el futuro port a Python.

## Extendiendo la estrategia

- Para agregar reglas de stop-loss o take-profit, reemplazar la llamada predeterminada `StartProtection()` con la configuración deseada.
- Se pueden crear módulos adicionales clonando el patrón de configuración `SignalModule`.
- Para la gestión avanzada de posiciones (por ejemplo, rastrear la exposición por módulo), se pueden agregar estrategias hijas o portafolios virtuales de StockSharp sobre esta base.
