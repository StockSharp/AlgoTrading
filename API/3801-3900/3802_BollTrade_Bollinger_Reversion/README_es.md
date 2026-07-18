# BollTrade Bollinger Estrategia de reversión
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La **Estrategia de reversión Bollinger de BollTrade** es una estrategia StockSharp de alto nivel convertida del clásico asesor experto de BollTrade MetaTrader. Opera con un único instrumento utilizando Bollinger bandas y espera las excursiones de precios más allá de las bandas más un buffer de pips adicional. Cuando una vela cierra por encima de la banda superior, la estrategia abre una posición corta, y cuando una vela cierra por debajo de la banda inferior, abre una posición larga. Todas las decisiones se toman sobre velas terminadas para evitar reaccionar ante datos incompletos.

## Lógica de trading

1. Suscríbase al tipo de vela configurado y calcule Bollinger Bandas con el período y la desviación seleccionados.
2. Calcule una compensación de precio adicional expresada en unidades de pips para imitar el colchón original que obligó a las operaciones a entrar más profundamente en territorio de sobrecompra/sobreventa.
3. Cuando el precio de cierre de una vela completa esté por debajo de la banda inferior menos el desplazamiento, abra una posición larga. Cuando esté por encima de la banda superior más el desplazamiento, abra una posición corta.
4. Para cada operación abierta, la estrategia almacena niveles de stop-loss y take-profit definidos en unidades de pips. Estas salidas emulan al asesor experto original que cerraba posiciones cuando las ganancias o pérdidas flotantes cruzaban distancias de pips predefinidas.
5. Las posiciones se cierran cuando el rango de velas cruza el umbral de stop-loss o take-profit. No se realiza ningún escalado ni piramidal adicional.

## Gestión monetaria

* El parámetro `Lots` define el tamaño de la posición base.
* Cuando `LotIncrease` está habilitado, el volumen aumenta proporcionalmente con el valor actual de la cartera en relación con el valor observado al inicio de la estrategia, hasta un límite de seguridad de 500 lotes. Esto reproduce la lógica de tallas vinculada al equilibrio de la versión MetaTrader.

## Parámetros

| Parámetro | Descripción |
|-----------|-------------|
| **Obtener ganancias (pips)** | Distancia en pips utilizada para calcular el nivel de obtención de beneficios a partir del precio de entrada. Establezca en cero para desactivar la salida de obtención de beneficios. |
| **Detener pérdidas (pips)** | Distancia en pips utilizada para calcular el nivel de stop-loss a partir del precio de entrada. Establezca en cero para desactivar la salida de stop-loss. |
| **Compensación de banda** | Distancia de pips adicional agregada más allá de la banda Bollinger antes de abrir una operación. |
| **Bollinger Período** | Número de velas utilizadas para la media móvil de Bollinger Bandas. |
| **Bollinger Desviación** | Multiplicador de desviación estándar para el ancho de bandas Bollinger. |
| **Volumen base** | Volumen de comercio base en lotes. |
| **Volumen de escala** | Cuando está habilitado, aumenta el volumen de pedidos en función del crecimiento del valor de la cartera. |
| **Tipo de vela** | Tipo de vela (período de tiempo) utilizado para la generación de señales. |

## Notas

* La estrategia funciona únicamente con velas terminadas y, por lo tanto, necesita datos históricos para prepararse antes de operar en vivo.
* Los niveles de stop-loss y take-profit se evalúan en rangos de velas, lo que se aproxima a la lógica original basada en ticks sin dejar de ser compatible con el nivel alto API.
* Las funciones de protección del marco StockSharp (`StartProtection`) están habilitadas para proteger contra la exposición accidental de la posición cuando la estrategia se detiene inesperadamente.
