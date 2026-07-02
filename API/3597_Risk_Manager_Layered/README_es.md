# Estrategia de protección de riesgos en capas
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
La **Estrategia de protección de riesgos en capas** es una conversión directa del MetaTrader asesor experto "RiskManager". El algoritmo rastrea continuamente la curva de capital de la cartera y ajusta la exposición al mercado utilizando el índice de canal de productos básicos (CCI), los múltiplos del rango verdadero promedio (ATR) y un modelo de tamaño de posición en capas. Cuando las métricas de riesgo caen por debajo de los umbrales configurables, la estrategia cambia automáticamente al modo de cobertura, cierra posiciones en eventos de ganancias o caídas y, opcionalmente, puede aplanarse hasta alcanzar el punto de equilibrio.

## Lógica de trading
- **Condiciones del indicador**: la estrategia se suscribe a la serie de velas primaria (período de tiempo configurable) y calcula:
  - CCI usando el período definido por el usuario. Las operaciones largas requieren que CCI caiga por debajo del umbral negativo y las operaciones cortas requieren que supere el umbral positivo.
  - ATR con un período fijo de 14 para derivar distancias de obtención de beneficios y stop-loss ajustadas por la volatilidad para cada capa abierta.
  - Un promedio móvil de volúmenes de velas. El comercio se habilita solo cuando el promedio móvil de las últimas 50 velas completadas excede el volumen de la vela anterior, replicando el filtro "Activo" original.
- **Entradas en capas**: la exposición máxima se distribuye en un número configurable de capas. Cada nuevo pedido utiliza el volumen por capa (`MaxVolume / Layers`). Las entradas adicionales se bloquean cuando el uso relativo de la capa (`Orders / Layers * 100`) excede el estado actual del sistema.
- **Gestión de pedidos**: cada capa abierta almacena su precio de entrada junto con niveles de stop-loss y take-profit basados en ATR. En cada vela completa se verifica el rango alto/bajo para decidir si alguna capa debe cerrarse debido a que alcanza sus niveles de protección.
- **Modo de cobertura**: cuando `MultiPairTrading` está deshabilitado y el porcentaje de salud calculado cae por debajo de `HedgeLevel`, la estrategia registra el recuento de órdenes actual y comienza a abrir capas del lado opuesto hasta que se alcanza el requisito del índice de cobertura. La cobertura se desactiva automáticamente una vez que la salud se recupera por encima del umbral.
- **Controles de capital**: varias protecciones reflejan el asesor experto original:
  - Stop duro de capital definido por `RiskLimit` (capital inicial menos límite de riesgo).
  - Objetivo de beneficio expresado como compensación aditiva sobre el capital inicial.
  - Nivel rodante de "capital cercano" que agrega `CloseProfitBuffer` al saldo actual cada vez que todas las posiciones se aplanan con éxito.
  - Salida de equilibrio opcional que cierra todas las operaciones cuando el capital alcanza el capital de equilibrio almacenado.
  - Interruptor manual de "Cierre Duro" que fuerza una posición plana inmediatamente.

## Parámetros
- `AllowLong` / `AllowShort`: habilita entradas largas o cortas respectivamente.
- `MaxVolume`: volumen total de posiciones asignado en todas las capas.
- `Layers` – Número máximo de capas que se pueden abrir simultáneamente.
- `CciLength` / `CciLevel`: período y umbral para el filtro CCI.
- `StopLossMultiple` / `TakeProfitMultiple` – multiplicador ATRes que definen los niveles de protección para cada capa.
- `CloseProfitBuffer`: beneficio agregado al saldo al reciclar el objetivo de cierre rodante. También se utiliza al calcular el capital de equilibrio.
- `ManualCapital`: anula el capital inicial utilizado para todos los cálculos de riesgo (establecido en cero para utilizar el saldo de la cartera activa al inicio).
- `RiskLimit` – Disposición máxima tolerada del capital inicial.
- `ProfitTarget`: objetivo de beneficio adicional que detiene las operaciones una vez alcanzado.
- `MultiPairTrading`: cuando es verdadero, la cobertura se desactiva incluso si la salud cae por debajo del límite.
- `HedgeLevel` / `HedgeRatio`: porcentaje de salud que inicia la cobertura y proporción de capas adicionales requeridas durante el modo de cobertura.
- `CloseAtBreakEven`: habilita la lógica de salida del punto de equilibrio.
- `HardClose`: fuerza el aplanamiento inmediato y detiene las operaciones adicionales mientras sea cierto.
- `CandleType` – Serie de velas utilizada para la evaluación de indicadores y decisiones comerciales.

## Notas
- La estrategia supone que se ejecutan órdenes de mercado inmediatas. Cuando se ejecuta con datos históricos, el modelo de ejecución real depende de la configuración de backtesting en StockSharp.
- La información sobre el patrimonio y el saldo proviene de la cartera conectada (`Portfolio.CurrentValue`, `Portfolio.CurrentBalance`). Asegúrese de que la cartera de estrategias esté sincronizada con el valor negociado.
- La cobertura abre posiciones de mercado adicionales sobre el mismo instrumento. Verificar que el broker o simulador permita posiciones opuestas cuando la cobertura esté habilitada.
- El seguimiento del punto de equilibrio reutiliza el valor `CloseProfitBuffer` tal como la versión original MetaTrader que operaba con un parámetro "ClosePL".
