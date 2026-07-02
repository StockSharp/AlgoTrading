# SpaceX Eliminar estrategia del botón StopLoss TakeProfit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia reproduce el botón **"ELIMINAR SL_TP"** del panel MetaTrader original *SpaceX_Delete_StopLoss_TakeProfit_button.mq5*. Está diseñado como una utilidad auxiliar que escanea la cartera actual y cancela todas las órdenes protectoras activas de stop-loss o take-profit que pertenecen a posiciones abiertas. La conversión se dirige al API de alto nivel de StockSharp y proporciona una manera conveniente de eliminar los soportes protectores sin abrir manualmente cada ticket.

La estrategia no abre ni cierra posiciones por sí sola. Simplemente inspecciona las operaciones ya abiertas y elimina sus órdenes de protección cuando se le indica que lo haga. Esto lo hace adecuado para operadores que administran posiciones manualmente o mediante otros sistemas automatizados pero desean un botón de pánico rápido que borre todas las órdenes de parada y toma de ganancias.

## Asesor experto original
La versión MetaTrader dibuja una única ventana de diálogo con un botón **ELIMINAR SL_TP**. Cada vez que se presiona el botón, el experto recorre todas las posiciones abiertas y llama a `PositionModify` con valores cero para stop-loss y take-profit. Como resultado, todos los niveles de protección se separan de la posición, mientras que el volumen de la posición permanece intacto.

Comportamientos clave del código fuente:

* No se crean entradas ni salidas de mercado.
* Todos los símbolos en el terminal se procesan sin filtrar.
* Sólo se eliminan los valores de stop-loss y take-profit; Los comentarios del pedido y los números mágicos permanecen intactos.
* La acción se activa exclusivamente mediante el botón GUI.

## StockSharp Implementación
La conversión StockSharp mantiene el comportamiento centrado en eliminar órdenes de protección. En lugar de un cuadro de diálogo GUI, la acción es impulsada por parámetros de estrategia que se pueden alternar desde la interfaz de usuario StockSharp o desde el código. La estrategia funciona con cualquier adaptador de corredor que exponga información de parada de orden o toma de ganancias.

Se admiten dos modos de ejecución:

1. **Ejecución automática al inicio** – opcional. Cuando está habilitada, la estrategia elimina las órdenes de protección inmediatamente después de que comienza a ejecutarse.
2. **Comando manual**: un parámetro booleano que imita el botón original. Establecer el parámetro en `true` programa una limpieza en el siguiente tic del temporizador, después de lo cual la bandera se restablece a `false`.

La conversión cancela las órdenes de protección llamando a `CancelOrder` en cada orden activa identificada como stop-loss, take-profit o cualquier otra orden de protección condicional. Los volúmenes de posición nunca se tocan.

## Parámetros
| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| **Ejecutar al inicio** (`ApplyOnStart`) | Cuando `true` la estrategia elimina las órdenes de protección inmediatamente después de que comienza la estrategia. | `true` |
| **Todos los valores** (`AffectAllSecurities`) | Procesa todas las posiciones de la cartera. Cuando `false` solo se considera la seguridad de la estrategia. | `true` |
| **Eliminar solicitud** (`DeleteRequest`) | Disparador manual que emula el botón MetaTrader. Gíralo a `true` para realizar una eliminación única; se reinicia automáticamente. | `false` |
| **Intervalo(s) de sondeo** (`PollingIntervalSeconds`) | Intervalo del temporizador en segundos utilizado para sondear el disparador manual. El temporizador también ejecuta la solicitud de eliminación cuando `Run On Start` está deshabilitado. | `1` |

## Cómo funciona
1. Al iniciar, la estrategia valida el intervalo de sondeo e inicia un temporizador que se activa cada *N* segundos.
2. Si **Ejecutar al iniciar** está habilitado, se ejecuta una limpieza inmediata.
3. Cada tic del temporizador marca la marca **Solicitud de eliminación**. Cuando la bandera es `true`, la estrategia recopila los valores que tienen posiciones abiertas dentro del alcance configurado y cancela todas las órdenes de protección para esos instrumentos.
4. Después de la ejecución, el indicador manual se restablece a `false`, lo que garantiza que la acción se ejecute solo una vez por solicitud.

### Identificación de órdenes de protección
Una orden se considera protectora cuando se cumple alguna de las siguientes condiciones:

* El tipo de orden es `Stop`, `TakeProfit` o `Conditional`.
* Está presente un precio de parada, un precio de toma de ganancias o una condición de orden no nula.

Esta definición conservadora cubre los adaptadores más comunes. Si un conector utiliza condiciones o tipos de órdenes personalizados para la gestión de paradas, amplíe la lógica de detección en consecuencia.

## Consejos de uso
* Adjunte la estrategia al conector que gestiona sus operaciones abiertas. Asegúrese de que todas las posiciones que desea administrar sean visibles para la cartera configurada.
* Active la solicitud de eliminación desde la cuadrícula de parámetros en Hydra o Terminal activando la casilla de verificación **Solicitud de eliminación**.
* Combine la utilidad con otras estrategias para eliminar temporalmente los soportes protectores antes de aplicar otros nuevos.
* Mantenga el intervalo de sondeo pequeño (1 segundo de forma predeterminada) para una experiencia de botón responsiva. Auméntelo si desea reducir la actividad del temporizador.

## Diferencias respecto al original EA
* El botón MetaTrader actúa instantáneamente a través de un cuadro de diálogo de gráfico. En StockSharp la acción se expone como un parámetro monitoreado por un temporizador.
* Las órdenes de protección se cancelan en lugar de modificar los objetos de posición. Este es el enfoque natural dentro de StockSharp porque los niveles de stop-loss y take-profit se representan como órdenes separadas en lugar de propiedades de posición en línea.
* El control de alcance opcional permite limitar la operación a la seguridad adjunta, lo cual es una conveniencia adicional en comparación con el experto original.

## Limitaciones
* La estrategia requiere que el adaptador exponga las órdenes de stop-loss y take-profit como órdenes activas. Si el corredor utiliza niveles de protección del lado del servidor que no están representados como órdenes, es posible que no sea posible cancelarlos.
* No se crea ningún cuadro de diálogo GUI. El control se realiza íntegramente a través de parámetros de estrategia o acceso programático.
* La utilidad no recrea niveles de protección; solo los elimina.

## Pruebas
La estrategia no incluye pruebas automatizadas dedicadas porque realiza funciones de utilidad sin cálculos complejos. Las pruebas manuales se pueden realizar abriendo posiciones de muestra, adjuntando la estrategia y verificando que todas las órdenes de protección se cancelen después de cada activación.
