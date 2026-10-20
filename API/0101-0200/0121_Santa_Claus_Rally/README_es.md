# Estrategia Santa Claus Rally
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Opera el rally de fin de año. Entra en la primera vela completa elegible de las últimas cinco sesiones de diciembre. Cierra al final de la segunda sesión de enero o en la primera vela posterior elegible si falta la vela de cierre.

Utiliza velas completas de 15 minutos y el calendario explícito de `Security.Board`, incluidos festivos, sesiones especiales y zona horaria del mercado. Sin calendario no puede iniciarse. Solo abre posiciones largas, con un intento de entrada por temporada anual; tras un stop no vuelve a entrar esa temporada. Las órdenes pendientes bloquean decisiones superpuestas.

`StopLoss = 2` configura protección nativa local del 2% desde el precio real de ejecución. Las actualizaciones bid/ask del instrumento pueden provocar una salida a mercado entre velas. No hay objetivo de beneficios.

La temporada se registra al enviar la entrada. La posición conserva el vencimiento de enero de esa temporada aunque la siguiente vela disponible llegue en diciembre de un año posterior; se cierra en esa vela sin trasladarse a otra temporada.
