# Estrategia de agente cercano
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Close Agent Strategy es un asistente de gestión de riesgos que refleja el asesor experto de MQL CloseAgent. La estrategia no abre nuevas operaciones. En cambio, monitorea las posiciones existentes y las cierra cuando el precio se extiende más allá de las bandas Bollinger mientras que el índice de fuerza relativa (RSI) alcanza zonas extremas. La herramienta puede detectar posiciones creadas manualmente o mediante otras estrategias automatizadas y, opcionalmente, liquidar todo una vez que se alcanza un objetivo de ganancias global.

## Indicadores y datos
- **Velas:** período de tiempo configurable (predeterminado: 5 minutos) utilizado para calcular los indicadores.
- **Bollinger Bandas (largo 21, ancho 2):** detecta variaciones de precios por encima de la banda superior o por debajo de la banda inferior.
- **Índice de Fuerza Relativa (longitud 13):** confirma si el mercado está sobrecomprado (>70) o sobrevendido (<30).
- **Datos de nivel 1:** captura la última oferta y solicita cotizaciones para evaluar las condiciones de salida con la mayor precisión posible.

## Parámetros
- **Modo de cierre (`CloseMode`):** selecciona qué posiciones son elegibles para cerrar.
  - `Manual` – solo posiciones sin este identificador de estrategia (operaciones manuales u otros bots).
  - `Auto`: solo posiciones abiertas por esta instancia de estrategia.
  - `Both`: monitorea cada posición en el símbolo de estrategia.
- **Tipo de vela (`CandleType`):** período de tiempo utilizado para calcular Bollinger Bandas y RSI.
- **Modo de operación (`OperationMode`):**
  - `LiveBar` – utiliza la última vela en formación; reacciona más rápido pero puede utilizar datos sin terminar.
  - `NewBar` – espera a que se cierre una vela antes de generar una señal (más seguro pero más lento).
- **Cerrar todo el objetivo (`CloseAllTarget`):** si la ganancia flotante (`PnL`) alcanza este valor absoluto, todas las posiciones monitoreadas se cierran inmediatamente.
- **Habilitar alertas (`EnableAlerts`):** cuando es verdadero, registra un mensaje cada vez que se activa una salida, incluida la estimación de ganancias obtenidas.

## Lógica de trading
1. Se suscribe a cotizaciones de Nivel 1 y a la serie de velas configuradas. Bollinger Bandas y RSI se actualizan para cada vela entrante.
2. Mantiene un búfer de historial compacto para que la estrategia pueda hacer referencia a la vela cerrada más reciente cuando `OperationMode` se establece en `NewBar`.
3. Comprueba si se alcanza el objetivo de beneficio global. Cuando `CloseAllTarget` > 0 y `PnL` supera el umbral, todas las posiciones elegibles se igualan a precios de mercado.
4. Para cada posición monitoreada en el símbolo de estrategia:
   - **Posiciones largas:** se cierran cuando la mejor oferta está por encima de la banda superior Bollinger, RSI está por encima de 70 y el precio se mantiene por encima del precio medio de entrada.
   - **Posiciones cortas:** se cierran cuando la mejor venta está por debajo de la banda Bollinger inferior, RSI está por debajo de 30 y el precio se mantiene por debajo del precio promedio de entrada.
5. Utiliza cotizaciones de oferta/demanda cuando estén disponibles; de lo contrario, vuelve al cierre de la última vela procesada para evitar salidas perdidas.

## Notas de uso
- La estrategia está diseñada como una capa protectora y supone que las posiciones podrían abrirse externamente.
- Dado que la lógica actúa únicamente sobre las salidas del mercado, la estrategia debe funcionar junto con otros sistemas comerciales para gestionar la exposición al riesgo.
- Las alertas aparecen en el registro del Diseñador cuando `EnableAlerts` está activo, coincidiendo con las alertas originales MQL.
