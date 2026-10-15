# Estrategia del programador de operaciones automáticas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

La estrategia AutoTrading Scheduler replica el asesor experto de EarnForex MetaTrader que activa el interruptor "AutoTrading" de MetaTrader. El puerto StockSharp mantiene la cuenta estable fuera de las ventanas de tiempo definidas por el usuario y reanuda las operaciones cuando el reloj retrocede dentro de un intervalo permitido. Toda la configuración se realiza mediante cadenas legibles, una para cada día de la semana.

El módulo es intencionalmente independiente de las señales: no abre nuevas operaciones por sí solo. En cambio, supervisa el estado comercial de la estrategia anfitriona. Cuando el programador desactiva el comercio automático, cancela todas las órdenes activas, opcionalmente aplana la posición actual y registra el evento a través de `AddInfoLog` para que la aplicación host pueda reaccionar.

## Lógica original

* Carga un horario persistente con múltiples intervalos de tiempo por día laborable.
* Admite bases de tiempo locales o de intermediario/servidor.
* Comprueba el horario cada segundo mediante un temporizador interno.
* Cuando el reloj está fuera de cada intervalo del día de la semana actual, desactiva el comercio automático y, opcionalmente, puede cerrar todas las operaciones abiertas y órdenes pendientes.
* Vuelve a habilitar el comercio automático una vez que el reloj ingresa nuevamente en cualquier intervalo permitido.

## Notas de implementación

* La versión StockSharp almacena la programación analizada en la memoria y la vuelve a calcular cada vez que el usuario edita uno de los parámetros de texto.
* Los intervalos de tiempo aceptan múltiples formatos: `9-12`, `09:30-16:00`, `21.15-23.45`. Los minutos son opcionales y el valor predeterminado es `00` cuando se omite. Separe varios tramos con comas.
* Un rango cuyo final es igual a `00:00` permanece activo hasta la medianoche (por ejemplo, `22-0` significa 22:00:00 hasta 23:59:59). El uso de `0-0` mantiene el comercio habilitado durante todo el día.
* Los períodos de tiempo cuyo final es menor que el inicio pasan automáticamente al día siguiente, reflejando la lógica de ayuda del asesor experto original.
* El cronómetro se ejecuta cada cinco segundos para equilibrar la capacidad de respuesta y el uso de recursos.

## Parámetros

| Nombre | Tipo | Predeterminado | Descripción |
| --- | --- | --- | --- |
| `SchedulerEnabled` | `bool` | `false` | Interruptor maestro que activa el horario. Cuando está deshabilitada, la estrategia nunca interfiere con el comercio. |
| `ReferenceClock` | `TimeReference` | `Local` | Elige entre el reloj de la máquina local y la hora del servidor/intercambio proporcionada por el conector. |
| `ClosePositionsBeforeDisable` | `bool` | `true` | Cuando el programador desactiva el comercio automático, primero cancela todas las órdenes activas y aplana la posición actual. |
| `MondaySchedule` | `string` | `""` | Lista separada por comas de intervalos comerciales para el lunes. |
| `TuesdaySchedule` | `string` | `""` | Lista separada por comas de intervalos comerciales del martes. |
| `WednesdaySchedule` | `string` | `""` | Lista separada por comas de intervalos comerciales para el miércoles. |
| `ThursdaySchedule` | `string` | `""` | Lista separada por comas de intervalos comerciales para el jueves. |
| `FridaySchedule` | `string` | `""` | Lista separada por comas de intervalos comerciales del viernes. |
| `SaturdaySchedule` | `string` | `""` | Lista separada por comas de intervalos comerciales para el sábado. |
| `SundaySchedule` | `string` | `""` | Lista separada por comas de intervalos comerciales para el domingo. |

Todos los parámetros de programación aceptan la misma sintaxis. Ejemplo: `"09-12, 13:30-17:45, 22-0"`.

## Uso

1. Adjunte la estrategia al valor o cartera deseada.
2. Ingrese uno o más rangos de tiempo para los días que desea operar. Deje un día vacío para prohibir el comercio durante todo el día.
3. Habilite el programador configurando `SchedulerEnabled = true`.
4. Decida si las posiciones deben aplanarse automáticamente usando `ClosePositionsBeforeDisable`.
5. Supervise la salida del registro: cada palanca escribe un mensaje con el motivo (ventana abierta o cerrada).

Cuando la hora actual está dentro de un rango permitido, la estrategia establece `IsAutoTradingEnabled = true`. Fuera de cada rango, la propiedad gira `false`, el módulo cancela las órdenes de trabajo, aplana la posición si está configurada y registra la acción.

## Limitaciones conocidas

* La estrategia solo supervisa el valor único que se le atribuye. Las carteras de múltiples símbolos requieren múltiples instancias de programador o un coordinador personalizado.
* El intervalo del temporizador se puede ajustar dentro del código fuente (`TimeSpan.FromSeconds(5)`) si se requiere una granularidad diferente.
* La estrategia no persiste la programación en el disco. Utilice los mecanismos de almacenamiento de parámetros de la aplicación host si es necesaria la persistencia.
