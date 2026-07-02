# Alerta Boring EA2
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Visión general
Boring EA2 Alert recrea la lógica de notificaciones del asesor experto MetaTrader 4 `boring-ea2`. La estrategia escucha velas terminadas, calcula tres medias móviles simples (SMA 3, SMA 20, SMA 150) y emite logs informativos cuando ocurre un cruce entre medias. La implementación evita intencionalmente colocar órdenes: el objetivo es dar alertas oportunas que los traders puedan combinar con ejecución discrecional u otras estrategias automatizadas.

## Lógica de estrategia
### Seguimiento de medias móviles
* **Sesgo de corto plazo:** una SMA de 3 periodos reacciona a cambios inmediatos de precio.
* **Tendencia media:** una SMA de 20 periodos suaviza el precio en el horizonte de swing de corto plazo.
* **Tendencia larga:** una SMA de 150 periodos representa el trasfondo dominante.

### Detección de cruces
* **SMA3 vs SMA20:** informa "crossed up" cuando SMA3 sube por encima de SMA20 y "crossed down" cuando cae por debajo. Banderas internas garantizan que cada transición se informe una vez.
* **SMA3 vs SMA150:** replica la misma lógica frente a la media de largo plazo para detectar impulsos de momentum o reversiones contra la tendencia dominante.
* **SMA20 vs SMA150:** añade una capa de confirmación de medio/largo plazo para que los cambios de estructura superior generen sus propias alertas.
* **Guarda de inicialización:** la primera vela terminada solo siembra el estado inicial. Las alertas empiezan con la segunda vela terminada cuando se observa un cambio real de relación.

### Formato de notificación
* Las alertas replican el mensaje del EA original: `Alert!!! - SYMBOL - TF - description`.
* El código de marco temporal se deriva del tipo de vela configurado. Se usan etiquetas estándar estilo MetaTrader (M1, M5, H1, etc.) cuando existen; otros marcos usan notación compacta (por ejemplo, `M45` o `D2`).
* Los mensajes se escriben con `AddInfoLog`, permitiendo enrutar a visores de log, scripts o dashboards GUI.

## Parámetros
* **Short SMA Length:** número de periodos de la media móvil rápida (predeterminado `3`).
* **Medium SMA Length:** número de periodos de la media móvil intermedia (predeterminado `20`).
* **Long SMA Length:** número de periodos de la media móvil lenta (predeterminado `150`).
* **Candle Type:** marco temporal usado para calcular las medias móviles. El valor predeterminado son velas de 1 minuto, igualando las comprobaciones por tick del EA con alta reactividad.

## Notas adicionales
* La estrategia no envía, modifica ni cancela órdenes. Es puramente informativa.
* Como `Bind` alimenta valores finalizados, cada cruce se evalúa sobre velas completadas. Esto evita los giros ruidosos intrabar que el EA original mitigaba contando ticks.
* Las notificaciones basadas en logging pueden integrarse con handlers personalizados suscribiéndose a eventos de log de la estrategia dentro de una aplicación anfitriona.
* No se proporciona traducción Python por ahora; solo la versión C# se incluye en el paquete API.
