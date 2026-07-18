# Estrategia Exp T3 TRIX (ID 2946)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia Exp T3 TRIX replica el asesor experto de MetaTrader 5 construido alrededor del oscilador TRIX de triple suavizado. Aplica el suavizado Tillson T3 para generar un flujo TRIX rápido y lento y reacciona a los cambios de momentum usando tres modos seleccionables. Cada modo controla cómo debe comportarse el histograma o la posición relativa de los componentes rápido y lento antes de que la estrategia entre o salga de una posición.

## Lógica de trading

- **Cálculo Tillson T3 TRIX**
  - Dos pilas de seis medias móviles exponenciales con la misma longitud producen valores Tillson T3 para un flujo rápido y uno lento.
  - La derivada de cada valor T3 (actual menos anterior dividido por anterior) se convierte en el histograma TRIX utilizado para la toma de decisiones.
- **Modo = Breakdown**
  - *Entrada larga*: La TRIX rápida cruza de por debajo de cero a por encima de cero mientras las entradas largas están habilitadas. Cualquier posición corta abierta se cierra primero (si se permiten las salidas cortas).
  - *Entrada corta*: La TRIX rápida cruza de por encima de cero a por debajo de cero mientras las entradas cortas están habilitadas. Cualquier posición larga abierta se cierra primero (si se permiten las salidas largas).
  - *Solo salida*: Cuando ocurre un cruce pero la entrada correspondiente está deshabilitada, la estrategia aún cierra la exposición opuesta si el permiso de salida relevante está habilitado.
- **Modo = Twist**
  - *Entrada larga*: La pendiente de la TRIX rápida cambia de negativa a positiva (es decir, la barra actual sube después de caer). La estrategia replica las reglas de cierre y permiso del modo Breakdown.
  - *Entrada corta*: La pendiente de la TRIX rápida cambia de positiva a negativa.
- **Modo = CloudTwist**
  - *Entrada larga*: La TRIX rápida se mueve por encima de la TRIX lenta después de estar por debajo de ella en la barra completada anterior.
  - *Entrada corta*: La TRIX rápida cae por debajo de la TRIX lenta después de estar por encima en la barra anterior.
- **Manejo de órdenes**
  - La estrategia primero cierra la exposición opuesta cuando aparece una señal de reversión y las salidas están permitidas.
  - Las nuevas órdenes usan `Volume + |Position|` para que una reversión pueda ejecutarse en una sola operación cuando sea permitido.
  - `StartProtection()` se activa para reutilizar la capa de seguridad integrada de StockSharp de la plantilla del proyecto original.

## Parámetros

| Parámetro | Valor predeterminado | Descripción |
|-----------|----------------------|-------------|
| `Fast Length` | 10 | Profundidad utilizada para la pila Tillson T3 rápida (seis EMAs enlazadas). |
| `Slow Length` | 18 | Profundidad utilizada para la pila Tillson T3 lenta. |
| `Volume Factor` | 0.7 | Coeficiente de suavizado Tillson T3 (0 a 1). |
| `Mode` | Twist | Elige entre detección de señales Breakdown, Twist o CloudTwist. |
| `Allow Long Entry` | true | Habilita la apertura de posiciones largas. |
| `Allow Short Entry` | true | Habilita la apertura de posiciones cortas. |
| `Allow Long Exit` | true | Habilita el cierre de posiciones largas. |
| `Allow Short Exit` | true | Habilita el cierre de posiciones cortas. |
| `Candle Type` | Marco temporal de 4 horas | Intervalo de agregación utilizado para solicitar velas y alimentar la cadena de indicadores. |

Todos los parámetros se exponen a través de `StrategyParam<T>`, haciéndolos visibles en la UI de Designer y listos para optimización.

## Notas de uso

1. La lógica solo funciona con velas finalizadas. Asegúrese de que la fuente de datos entregue el marco temporal configurado en `Candle Type`.
2. Dado que la derivada TRIX requiere valores históricos, las dos primeras velas completadas se usan para inicialización y no producen señales.
3. Para replicar el comportamiento de MetaTrader, deshabilite el flag `Allow ...` correspondiente si desea trading unidireccional o supresión de salidas.
4. La gestión de riesgos como niveles de stop-loss o take-profit no se incluyó en el asesor experto original y por lo tanto no se implementa aquí. Combine la estrategia con los módulos de gestión de dinero de StockSharp si es necesario.

## Detalles de conversión

- Fuente: `MQL/2156/exp_t3_trix.mq5` más el indicador `t3_trix.mq5`.
- El port de API implementa los mismos tres modos de señal utilizando suscripciones de velas de alto nivel de StockSharp y clases de indicadores.
- El suavizado Tillson T3 se recrea usando seis medias móviles exponenciales encadenadas y el factor de volumen canónico de 0.7, ajustable a través de `Volume Factor`.
