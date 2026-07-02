# Juego de ruleta
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia del juego de ruleta recrea el asesor experto similar a un casino de MetaTrader dentro de StockSharp. Trata cada vela terminada como un nuevo giro de la rueda, elige una dirección aleatoria y escala el tamaño de su orden después de las pérdidas usando una progresión de estilo Martingale. La implementación realiza un seguimiento de un bankroll virtual y limita la exposición mediante límites configurables.

Cada ronda comienza aplanando cualquier posición existente, lanzando una moneda virtual para representar el rojo o el negro y enviando una orden de mercado en la dirección seleccionada. Cuando se cierra la siguiente vela, la estrategia comprueba si el cierre se movió a favor de la apuesta. Las ganancias restablecen la apuesta al volumen base, mientras que las pérdidas multiplican la apuesta hasta un límite definido. Una guardia de racha perdedora máxima obliga a reiniciar antes de que la exposición se vuelva extrema. Se pueden insertar velas de enfriamiento opcionales entre rondas para ralentizar el ritmo de las apuestas.

Esta conversión se centra en la gestión del dinero inspirada en los juegos de azar mostrada por el experto original en lugar de en las señales de los indicadores. Demuestra cómo orquestar rondas basadas en tiempo, mantener el estado interno e interactuar con API de alto nivel de StockSharp a través de suscripciones de velas.

## Detalles

- **Criterios de inscripción**: Sin filtro técnico. La dirección se selecciona aleatoriamente al final de una vela terminada.
- **Largo/Corto**: Ambas direcciones, elegidas al azar en cada ronda.
- **Criterios de Salida**: La posición se cierra en la siguiente vela terminada, evaluando si el precio cerró por encima o por debajo de la entrada.
- **Paradas**: No hay paradas tradicionales. El riesgo se gestiona con límites de participación y límites de racha.
- **Valores predeterminados**:
  - `BaseVolume` = 1 metro
  - `LossMultiplier` = 2m
  - `MaxMultiplier` = 16m
  - `RoundCooldown` = 1
  - `MaxLosingStreak` = 5
  - `CandleType` = Intervalo de tiempo.DesdeMinutos(1)
- **Filtros**:
  - Categoría: Gestión del dinero
  - Dirección: Ambos
  - Indicadores: Ninguno
  - Paradas: No
  - Complejidad: Principiante
  - Plazo: Corto plazo
  - Estacionalidad: No
  - Redes neuronales: No
  - Divergencia: No
  - Nivel de riesgo: alto

## Notas

- Las órdenes de mercado se dimensionan según la apuesta ajustada por el multiplicador y se redondean al paso de volumen del instrumento.
- Las ganancias restablecen la apuesta al volumen base; las pérdidas aumentan según el multiplicador hasta alcanzar el multiplicador máximo o el límite de racha de pérdidas.
- Las barras de enfriamiento evitan el reingreso inmediato y permiten la sincronización con fuentes de datos más lentas.
