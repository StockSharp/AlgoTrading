# Estrategia Color 3rdGen XMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia opera basándose en la dirección de una media móvil de tercera generación. El indicador es una combinación de dos medias móviles exponenciales y se vuelve azul cuando sube y rosa cuando baja. Se registra una señal de compra cuando la media gira hacia arriba, y una señal de venta cuando gira hacia abajo.

Las órdenes se colocan solo en un horario especificado por el usuario después de que aparece una señal. Las posiciones también pueden cerrarse cuando se detecta la señal opuesta o cuando vence un período de tenencia predefinido. Los niveles opcionales de stop-loss y take-profit se miden en puntos.

## Parámetros

- **Length** – período de suavizado de la media de tercera generación.
- **StartHour** – hora en que se pueden abrir nuevas posiciones.
- **StartMinute** – minuto dentro de la hora en que se permiten aperturas.
- **HoldMinutes** – tiempo máximo para mantener una posición abierta.
- **Volume** – volumen de orden utilizado para las entradas.
- **StopLoss** – distancia de stop-loss en puntos. `0` deshabilita el stop.
- **TakeProfit** – distancia de take-profit en puntos. `0` deshabilita el objetivo.
- **UseLongEntries** – habilitar entradas largas.
- **UseShortEntries** – habilitar entradas cortas.
- **CloseLongBySignal** – cerrar posiciones largas cuando aparece una señal de venta.
- **CloseShortBySignal** – cerrar posiciones cortas cuando aparece una señal de compra.
- **CandleType** – marco temporal de las velas usadas para cálculos.

## Lógica

1. Suscribirse a velas del marco temporal seleccionado.
2. Calcular la media móvil de tercera generación para cada vela.
3. Detectar cuándo la media sube o baja entre velas consecutivas.
4. Almacenar una señal de compra o venta basada en el cambio de dirección.
5. En el horario de apertura especificado, entrar en la dirección de la señal almacenada.
6. Cerrar posiciones en señales opuestas, cuando transcurre el tiempo de tenencia, o cuando se alcanzan los niveles de stop-loss/take-profit.
