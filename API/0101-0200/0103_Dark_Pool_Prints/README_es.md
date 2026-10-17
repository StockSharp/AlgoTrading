# Estrategia de Impresiones de Dark Pool
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera informes de grandes transacciones extrabursátiles según el lado agresor comunicado, con la SMA20 del instrumento negociado como filtro de tendencia.

`Security` identifica el instrumento negociado y `DarkPoolSecurity` un flujo independiente de informes del mismo subyacente. Sus identificadores deben ser distintos. El flujo debe aportar operaciones extrabursátiles reales (`IsSystem = false`), precio, volumen, fecha y `OriginSide`. La ausencia del flujo o un identificador idéntico impiden el inicio. Las operaciones bursátiles, informes sin lado, valores inválidos, duplicados e informes fuera de orden no generan entradas.

El volumen debe superar estrictamente `1.5 × media de los 20 informes anteriores`; el informe actual queda excluido. Compra ante una compra comunicada por encima de SMA20 y vende ante una venta por debajo. El color y volumen de las velas no sustituyen los datos del informe. Una entrada contraria revierte toda la posición ejecutada con `Volume + abs(Position)`.

La SMA usa velas completas de 15 minutos. Cierra largos por debajo de SMA20 y cortos por encima, con independencia del volumen de informes y de la pausa entre entradas. `CooldownBars = 0` por defecto; un valor positivo retrasa las siguientes entradas ese número de velas completas. Una orden pendiente bloquea señales superpuestas.

`StopLoss = 2` configura protección nativa local del 2% desde el precio real de ejecución. Sigue bid/ask del instrumento y envía una salida a mercado; necesita cotizaciones entre velas. No hay objetivo de beneficios.
