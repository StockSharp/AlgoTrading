# Estrategia Gandalf PRO
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia replica el asesor experto Gandalf PRO de MQL. Calcula dos medias móviles (LWMA y SMA) sobre el cierre de las velas
y las introduce en la lógica de suavizado recursivo original para proyectar un precio objetivo futuro. Cuando el objetivo proyectado
se aleja lo suficiente del cierre actual (15 pasos de precio o más), la estrategia abre una orden de mercado y almacena los niveles
de stop-loss y take-profit derivados del pronóstico.

Las operaciones largas requieren que el objetivo suavizado esté por encima del cierre actual en al menos 15 pasos; las operaciones
cortas requieren que el objetivo esté por debajo del cierre en el mismo margen. Los stop-loss se definen en pasos de precio y se
convierten usando el paso de precio del instrumento. Los niveles de take-profit son iguales al objetivo proyectado y se monitorean
en cada vela completada. Los multiplicadores de riesgo reescalan el volumen base de la estrategia, permitiendo reglas simples de
gestión monetaria.

## Parámetros
- Tipo de vela
- Activar compra
- Longitud de compra
- Factor de precio de compra
- Factor de tendencia de compra
- Stop-loss de compra
- Multiplicador de riesgo de compra
- Activar venta
- Longitud de venta
- Factor de precio de venta
- Factor de tendencia de venta
- Stop-loss de venta
- Multiplicador de riesgo de venta
