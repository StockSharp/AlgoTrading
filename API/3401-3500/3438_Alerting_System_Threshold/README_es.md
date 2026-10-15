# Estrategia de umbral del sistema de alerta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de umbral del sistema de alertas** es un puerto StockSharp del asesor experto MetaTrader 5 "AlertingSystem" (MQL carpeta `31843`). El EA original dibuja dos líneas horizontales y reproduce un sonido cada vez que la oferta cotiza por encima de la línea superior o la demanda por debajo de la línea inferior. Esta conversión de C# mantiene el comportamiento de alerta mientras utiliza el nivel alto API de StockSharp para el acceso a datos y el registro de notificaciones.

## Idea central

* Escuche datos de mercado de Nivel 1 en tiempo real (mejor oferta y mejor demanda).
* Active alertas únicas cuando la oferta sea mayor o igual a un umbral superior configurable.
* Active alertas únicas cuando la solicitud sea menor o igual a un umbral inferior configurable.
* Restablezca las banderas de alerta cuando los precios regresen dentro de la banda para que se pueda detectar la próxima ruptura.

A diferencia de la implementación MQL que reproduce repetidamente un sonido en cada tic, la versión StockSharp envía una única entrada de registro informativo para cada evento de ruptura. Esto evita la inundación de registros y al mismo tiempo notifica al operador cuando se alcanzan los objetivos de precios.

## Parámetros

| Parámetro | Descripción | Predeterminado |
|-----------|-------------|---------|
| `UpperPrice` | Nivel de oferta que activa la alerta alcista. Establezca en `0` para desactivar. | `0` |
| `LowerPrice` | Nivel de pregunta que activa la alerta bajista. Establezca en `0` para desactivar. | `0` |

Ambos parámetros son valores `StrategyParam<decimal>` estándar que se pueden optimizar o ajustar en tiempo de ejecución. Puede mover los umbrales durante las operaciones en vivo tal como lo haría con las líneas horizontales en MetaTrader.

## Suscripciones de datos y flujo de trabajo

1. Cuando comienza la estrategia, se suscribe a los datos de Nivel 1 a través de `SubscribeLevel1().Bind(ProcessLevel1).Start()`.
2. Los objetos `Level1ChangeMessage` entrantes actualizan los valores de mejor oferta y mejor demanda almacenados en caché.
3. Cada actualización llama a las comprobaciones de alerta:
   * **Alerta superior**: se activa una vez cuando `BestBid >= UpperPrice` y el precio estaban previamente por debajo del nivel.
   * **Alerta inferior**: se activa una vez cuando `BestAsk <= LowerPrice` y el precio estaban previamente por encima del nivel.
4. Los reinicios de alerta se producen automáticamente cuando el mercado vuelve a cotizar dentro del corredor.

## Registro y notificaciones

Las alertas se escriben con `AddInfoLog` e incluyen los valores de oferta/demanda actuales y los niveles configurados. Integre su propio canal de notificaciones (correos electrónicos, mensajería, sonidos personalizados) anulando `OnInfo` o suscribiéndose a los eventos de registro de la estrategia en su aplicación de alojamiento.

## Consejos de uso

* Establezca solo los umbrales que le interesen; el otro puede permanecer `0` para permanecer deshabilitado.
* Combine la estrategia con otros módulos que reaccionan a los registros `Info` si desea reproducir notificaciones audibles o push.
* Como la estrategia nunca realiza pedidos, no es necesario llamar a `StartProtection()`.

## Diferencias con el original EA

* La versión StockSharp utiliza datos de Nivel 1 en lugar de crear objetos de gráfico.
* Las alertas son únicas por ruptura para mantener limpio el registro.
* Todo lo demás (parámetros, umbrales lógicos, condiciones) coincide con la referencia MQL.

## Archivos

* `CS/AlertingSystemStrategy.cs` – Implementación de la estrategia C#.
* `README.md` – Documentación en inglés (este archivo).
* `README_ru.md` – Traducción al ruso con explicación adicional.
* `README_zh.md` – Traducción al chino simplificado.
