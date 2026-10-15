# Conectar Desconectar Estrategia de alerta sonora
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de alerta de sonido de conexión y desconexión** monitorea continuamente el estado de conexión del conector de estrategia y registra cada transición entre los estados en línea y fuera de línea. El experto original de MQL5 reproducía archivos de audio cuando el terminal MetaTrader estaba conectado o desconectado. Esta conversión de C# mantiene la lógica central (detección de cambios de conexión) y expone enlaces que permiten que el tiempo de ejecución StockSharp registre eventos y duraciones. La estrategia se puede utilizar como un perro guardián ligero que informa al operador sobre problemas de conectividad sin realizar ningún pedido.

## Características clave
- Sondea periódicamente el estado del conector utilizando un intervalo configurable.
- Detecta eventos de conexión y desconexión y escribe entradas de registro detalladas.
- Registra cuánto tiempo permaneció el terminal en línea o fuera de línea (opcional).
- Omite los sonidos de notificación en la primera verificación para reflejar el comportamiento de MQL.

## Parámetros
| Nombre | Predeterminado | Descripción |
| ---- | ------- | ----------- |
| `CheckIntervalSeconds` | `1` | Número de segundos entre comprobaciones del estado del conector. Debe ser mayor que cero. |
| `LogDurations` | `true` | Cuando está habilitada, la estrategia registra el período de tiempo que la conexión permaneció en línea o fuera de línea después de cada transición. |

Todos los parámetros están expuestos a través de `StrategyParam<T>` para que puedan modificarse desde la interfaz de usuario o durante la optimización.

## Cómo funciona
1. Cuando se inicia la estrategia, almacena el estado actual del conector y, opcionalmente, registra el estado inicial.
2. Un `System.Threading.Timer` llama periódicamente a un controlador interno que compara el indicador de conexión actual con el valor anterior.
3. Si el estado cambió, la estrategia registra la transición. La primera notificación está marcada como "inicial" y no representa una alerta sonora real (que coincide con la lógica del asesor experto original).
4. Los registros de duración opcionales muestran cuánto duró el estado anterior, lo que ayuda al operador a evaluar la estabilidad de la conexión.
5. El temporizador se elimina automáticamente cuando la estrategia se detiene o se reinicia.

## Notas de uso
- Adjunte la estrategia a cualquier terminal StockSharp habilitado para conector. No interactúa con datos de mercado ni realiza pedidos.
- Mantenga el intervalo de sondeo predeterminado para un monitoreo casi en tiempo real. Aumente el valor si solo necesita actualizaciones generales.
- La estrategia utiliza el subsistema de registro StockSharp (`LogInfo`). Configure detectores de registros o paneles para ver las notificaciones.
- Para agregar alertas de sonido reales, conecte un servicio de notificación en su aplicación host y reproduzca audio cuando lleguen los mensajes de registro.

## Consideraciones de seguridad
- La estrategia valida el intervalo de sondeo y genera una excepción si no es positivo.
- Las devoluciones de llamadas del temporizador utilizan la estrategia `CurrentTime` para garantizar marcas de tiempo consistentes incluso cuando se utiliza la reproducción de datos históricos.
- Todos los recursos se liberan al detener/reiniciar para evitar temporizadores en segundo plano después de que se desactiva la estrategia.
