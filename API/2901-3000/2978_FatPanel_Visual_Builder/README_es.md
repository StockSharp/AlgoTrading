# Estrategia FatPanel Constructor Visual
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia FatPanel Constructor Visual** es una traducción de StockSharp del Asesor Experto FAT Panel heredado de MetaTrader. La implementación MQL original exponía un lienzo de arrastrar y soltar donde los usuarios podían vincular bloques de indicadores, lógica, estado y órdenes. Este port en C# mantiene la filosofía modular pero expresa cada conexión de bloque a través de un único documento JSON que la estrategia lee al arrancar.

## Cómo funciona la conversión

* El panel MQL creaba botones, pestañas y un despachador basado en temporizador. Esas preocupaciones de UI se eliminan por completo. En cambio, la estrategia analiza el parámetro `Configuration` (una cadena JSON) e instancia los bloques de señal y lógica correspondientes internamente.
* Los bloques se evalúan en cada vela finalizada del `CandleType` configurado. Los bloques de indicadores usan indicadores de StockSharp (`SMA`, `EMA`, `SMMA`, `WMA`) y nunca dependen de búferes manuales.
* Los bloques de órdenes originales permitían la selección de símbolo, stop-loss y take-profit en "puntos". En StockSharp, la seguridad predeterminada se toma de `Strategy.Security`; el stop-loss y take-profit se reintroducen a través de los parámetros de estrategia `StopLossPoints` y `TakeProfitPoints` y se convierten a distancias de precio absolutas usando `Security.PriceStep`.
* Los filtros de tiempo y día de la semana reflejan la lógica MQL. La señal de precio de oferta suscribe a datos Level1 solo si al menos una regla lo solicita, replicando el comportamiento de actualización bajo demanda del despachador del panel.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `CandleType` | Tipo de datos y marco temporal que alimenta cada señal. |
| `Configuration` | Documento JSON que describe reglas, condiciones y acciones. El valor predeterminado reproduce la estrategia de cruce EMA/SMA de muestra del panel. |
| `Volume` | Tamaño de orden predeterminado usado por las acciones a menos que una regla lo anule. |
| `StopLossPoints` | Distancia en pasos de precio para la protección de riesgo incorporada. Establecer en `0` para deshabilitar el stop-loss. |
| `TakeProfitPoints` | Distancia en pasos de precio para el take-profit incorporado. Establecer en `0` para deshabilitar. |

`StopLossPoints` y `TakeProfitPoints` solo se activan cuando se proporciona un valor positivo **y** la seguridad expone un `PriceStep` válido.

## Estructura de configuración

El esquema JSON está diseñado para mantenerse cercano al lenguaje de bloques del FAT Panel:

```json
{
  "rules": [
    {
      "name": "Nombre de regla (opcional)",
      "all": [ /* condiciones que deben ser todas verdaderas */ ],
      "any": [ /* condiciones opcionales, al menos una debe ser verdadera */ ],
      "none": [ /* condiciones opcionales que deben ser todas falsas */ ],
      "action": { "type": "Buy" | "SellShort" | "Close", "volume": 1.0 }
    }
  ]
}
```

Cada elemento de condición tiene un campo `type` con uno de los siguientes valores:

| Tipo | Campos JSON | Propósito |
| --- | --- | --- |
| `comparison` | `operator`, `left`, `right`, `threshold` | Conecta dos bloques de señal a través de operadores lógicos (`Greater`, `Less`, `Equal`, `CrossAbove`, `CrossBelow`). Los umbrales se interpretan como diferencias de precio absolutas. Los operadores de cruce se activan cuando la vela anterior estaba en el lado opuesto y la diferencia actual supera el umbral. |
| `position` | `required` | Refleja los bloques de estado del panel FAT (`Any`, `FlatOnly`, `FlatOrShort`, `FlatOrLong`, `LongOnly`, `ShortOnly`). |
| `time` | `start`, `end` | Filtro de sesión intradía en formato `HH:mm`. Inicio > fin mantiene el comportamiento nocturno del panel MQL. |
| `dayOfWeek` | `days` | Lista de nombres de días. Cuando se omite la condición por defecto es de lunes a viernes, coincidiendo con los valores predeterminados del panel. |

Las señales (`left` / `right`) se definen como:

```json
{ "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" }
{ "type": "Bid" }
{ "type": "Constant", "level": 1.2345 }
```

* `MovingAverage` admite métodos `Simple`, `Exponential`, `Smoothed` y `LinearWeighted` con cualquiera de las fuentes de precio OHLC. El indicador comparte el flujo de velas de la estrategia, tal como el panel usaba marcos temporales seleccionados en el gráfico.
* `Bid` usa el último mejor precio de oferta de las actualizaciones de level1 (recurre al cierre de la vela hasta que llegue una cotización).
* `Constant` reproduce el bloque HLINE y produce un nivel estático.

Las acciones de reglas replican los bloques de órdenes:

* `Buy` – abre o revierte a una posición larga cuando la posición actual es plana o corta.
* `SellShort` – abre o revierte a una posición corta cuando la posición es plana o larga.
* `Close` – sale de cualquier posición abierta usando `ClosePosition()`.

Un `volume` por acción puede anular el parámetro `Volume` predeterminado.

## Flujo de ejecución

1. Cuando la estrategia arranca analiza el JSON de configuración. Los documentos inválidos detienen la estrategia y emiten un registro de error.
2. Los indicadores se instancian y almacenan en caché para que múltiples reglas puedan reutilizar las mismas definiciones de señal sin cálculos duplicados.
3. Para cada vela finalizada la estrategia actualiza los valores de señal y luego evalúa cada regla en orden. Las condiciones `all` deben pasar, `any` debe pasar al menos una vez (si se proporciona), y `none` debe fallar completamente.
4. Si se activa una acción la estrategia registra el nombre de la regla y ejecuta la orden de mercado solicitada.
5. Las protecciones opcionales de stop-loss y take-profit se activan una vez durante `OnStarted` usando las distancias en puntos suministradas.

## Limitaciones y notas

* Solo se admite la `Strategy.Security` principal. El enrutamiento entre símbolos del panel original requeriría múltiples instancias de estrategia.
* El despachador MQL permitía el anidamiento profundo de bloques de lógica (por ejemplo, AND dentro de OR). La estructura JSON proporciona control similar a través de los arrays `all`/`any`/`none`, pero los gráficos extremadamente complejos aún pueden necesitar adaptación manual.
* El operador `Cross` usa solo la última vela. El bloque MQL exponía un búfer de retroceso y delta en "puntos"; adapte el campo `threshold` para emular la sensibilidad requerida.
* Las características de UI como posiciones de arrastre, ventanas de diálogo e iconos de barra de herramientas no tienen equivalente directo en StockSharp y se omiten intencionalmente.

## Configuración de muestra

La configuración predeterminada integrada en la estrategia se reproduce a continuación para mayor comodidad:

```json
{
  "rules": [
    {
      "name": "EMA crosses above SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossAbove",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "dayOfWeek", "days": ["Monday", "Tuesday", "Wednesday", "Thursday", "Friday"] },
        { "type": "time", "start": "09:00", "end": "17:00" },
        { "type": "position", "required": "FlatOrShort" }
      ],
      "action": { "type": "Buy" }
    },
    {
      "name": "EMA crosses below SMA",
      "all": [
        {
          "type": "comparison",
          "operator": "CrossBelow",
          "left": { "type": "MovingAverage", "period": 20, "method": "Exponential", "price": "Close" },
          "right": { "type": "MovingAverage", "period": 50, "method": "Simple", "price": "Close" }
        },
        { "type": "position", "required": "LongOnly" }
      ],
      "action": { "type": "Close" }
    }
  ]
}
```

Esta muestra refleja la plantilla de panel de acciones: abrir una posición larga en un cruce alcista de EMA 20/50 con SMA durante la sesión regular y cerrar la posición en el cruce inverso.
