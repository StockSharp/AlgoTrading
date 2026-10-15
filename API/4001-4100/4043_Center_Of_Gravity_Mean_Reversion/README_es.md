# Estrategia de reversión de la media del centro de gravedad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia reconstruye el canal del centro de gravedad utilizado por el experto original MQL4 resolviendo una regresión polinómica en las velas más recientes. El centro de regresión se calcula a partir de la intersección del ajuste de mínimos cuadrados, mientras que el ancho de la banda se deriva de la desviación estándar de los precios de cierre en el mismo horizonte retrospectivo. La banda inferior es igual al centro de regresión menos la desviación escalada, lo que reproduce el búfer `stdl` al que se accede en el robot de origen.

Durante el procesamiento en vivo, el algoritmo mantiene una cola continua de cierres con la longitud definida por el parámetro **Bars Back**. Cada vela terminada desencadena un nuevo cálculo de los coeficientes de regresión mediante eliminación gaussiana en el sistema de ecuaciones normal. Esto evita almacenar historiales completos de velas pero produce la misma geometría de canal que el indicador personalizado. Si la matriz queda mal acondicionada, se omite la actualización, lo que evita decisiones comerciales inestables.

La lógica comercial refleja la del experto original: cuando el mínimo de la vela actual se mantiene por encima de la banda de desviación inferior (`lowerBand < Low` en notación MQL), la estrategia lo considera un rebote de reversión a la media. Si no hay ninguna posición larga abierta, cualquier exposición corta se cierra y se emite una orden de compra de mercado utilizando el volumen de la estrategia. Los valores inferior, superior y central más recientes se exponen mediante propiedades de solo lectura para gráficos o diagnósticos.

La gestión de riesgos es opcional. **Distancia Stop Loss** y **Distancia Take Profit** se especifican en unidades de precio absoluto. Cuando es distinto de cero, la estrategia registra el precio de entrada de la posición larga activa y verifica los extremos de las velas para determinar si se ha alcanzado un objetivo de parada o de ganancias. Si no se proporciona ninguno de los parámetros, la posición se puede gestionar manualmente o mediante módulos externos.

### Parámetros
- **Tipo de vela**: período de tiempo de la suscripción de la vela que alimenta la regresión.
- **Bars Back**: número de barras históricas utilizadas para calcular el canal de regresión (predeterminado 125).
- **Grado polinómico**: grado de la regresión polinómica (predeterminado 2) que rige la curvatura del canal.
- **Std Multiplier**: multiplicador aplicado a la desviación estándar al formar la envolvente (predeterminado 1).
- **Distancia de Stop Loss**: compensación opcional de stop loss largo en unidades de precio (el valor predeterminado 0 lo desactiva).
- **Distancia de obtención de beneficios**: compensación opcional de obtención de beneficios a largo plazo en unidades de precio (el valor predeterminado 0 lo desactiva).

La estrategia opera únicamente con velas completadas, utiliza órdenes de mercado para las entradas y no realiza ventas en corto automáticas porque la rama de venta del experto original fue comentada.
