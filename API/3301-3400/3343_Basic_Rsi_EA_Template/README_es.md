# Estrategia de plantilla básica RSI EA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La **Estrategia de plantilla RSI EA básica** replica el asesor experto MetaTrader 4 "Basic Rsi EA Template.mq4" (MQL/26750). Observa el índice de fuerza relativa (RSI) en la serie de velas seleccionada y reacciona cuando el impulso se extiende hacia zonas configurables de sobrecompra o sobreventa. La conversión StockSharp mantiene el flujo de trabajo simple de una posición y la lógica protectora de parada/toma del robot original al tiempo que adopta la suscripción de alto nivel API.

## Lógica de la estrategia

### Indicadores
- **Índice de fuerza relativa (RSI)** con un período configurable calculado sobre el tipo de vela elegido.

### Condiciones de entrada
- **Configuración larga**: cuando RSI cae por debajo de `OversoldLevel` y la estrategia no tiene ninguna posición abierta, envía una orden de compra de mercado para el `OrderVolume` configurado.
- **Configuración corta**: cuando RSI sube por encima de `OverboughtLevel` y la estrategia no tiene ninguna posición abierta, envía una orden de venta de mercado para el `OrderVolume` configurado.

El algoritmo funciona en modo de compensación: sólo puede existir una posición en cualquier momento. Si una posición larga está activa, la estrategia espera a que se cierre antes de una entrada corta (y viceversa).

### Condiciones de salida
- **Parada de protección**: `StopLossPips` se convierte en una distancia de precio absoluta utilizando el tamaño del tick del instrumento. Una vez que el precio retrocede esa cantidad, el motor de protección incorporado cierra la posición.
- **Obtener ganancias**: `TakeProfitPips` se procesa de la misma manera: cuando el precio se mueve a favor en la distancia configurada, la posición se cierra para obtener ganancias.

No hay ninguna salida adicional basada en señales o de seguimiento. La estrategia se basa únicamente en distancias de protección o intervención manual para salir de las operaciones, reflejando el diseño minimalista de la plantilla original.

### Manejo de riesgos y volúmenes
- `OrderVolume` define la cantidad fija enviada con cada orden de mercado (lotes predeterminados de 0,01, que coinciden con la muestra MQL).
- La estrategia no es piramidal ni de cobertura. Si una parada protectora o una toma de ganancias cierra la operación activa, el algoritmo se vuelve plano y espera el siguiente activador RSI.

## Parámetros
- `CandleType`: serie de velas utilizadas para la generación de señales (predeterminado: período de tiempo de 1 minuto).
- `RsiPeriod`: número de barras en la ventana RSI (por defecto: 14).
- `OverboughtLevel`: RSI umbral que permite entradas cortas (predeterminado: 70).
- `OversoldLevel`: RSI umbral que permite entradas largas (predeterminado: 30).
- `StopLossPips`: distancia de parada en pips convertida a unidades de precio absoluto (predeterminado: 30 pips).
- `TakeProfitPips`: objetivo de beneficio en pips convertido a unidades de precio absoluto (predeterminado: 20 pips).
- `OrderVolume`: volumen fijo para órdenes de mercado (por defecto: 0,01).

## Notas de implementación
- Utiliza `SubscribeCandles(...).Bind(rsi, ProcessCandle)` para que los valores del indicador fluyan directamente al método de procesamiento sin administración manual del búfer.
- `CreateProtectionUnit` recrea el manejo de pips de MQL: los instrumentos con 3 o 5 decimales utilizan un multiplicador de 10x para asignar pips a pasos de precio.
- Todas las comprobaciones de indicadores se ejecutan en velas terminadas para evitar múltiples órdenes en la misma barra.
- La conversión supone una cuenta de compensación, a diferencia del modo de cobertura de MetaTrader. En consecuencia, las operaciones opuestas cierran la posición actual en lugar de crear múltiples tickets.
- Los comentarios y registros en línea están en inglés para ayudar en el mantenimiento futuro.

## Consejos de uso
- Ajuste `CandleType` al instrumento y período de tiempo que desea operar (por ejemplo, cambie a velas horarias para configuraciones de swing).
- Ajuste `StopLossPips` y `TakeProfitPips` para que coincidan con la volatilidad del instrumento; las distancias de protección son fundamentales para el control de riesgos.
- Combine la estrategia con StockSharp cartera o módulos de riesgo si necesita una gestión avanzada del dinero más allá de la lógica de la plantilla.
