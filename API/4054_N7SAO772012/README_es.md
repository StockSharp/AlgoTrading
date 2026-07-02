# N7S AO 772012 Estrategia
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general

Esta estrategia es una conversión StockSharp del asesor experto MetaTrader **N7S_AO_772012**. El robot original combina filtros tipo perceptrón del Awesome Oscillator (AO) en varios períodos de tiempo con una puerta de patrón de precios y un modo "neuro" configurable que puede anular la lógica base. La versión convertida conserva el árbol de decisión al tiempo que expone todos los controles de ajuste como parámetros de estrategia.

El bot opera en el instrumento principal seleccionado en la estrategia y utiliza:

- **Velas M1** para el momento de entrada y el perceptrón de precios.
- **Velas H1** para alimentar múltiples perceptrones basados en AO.
- **Velas H4** para calcular el delta de impulso AO utilizado por el selector base de Compra/Venta (BTS).

## Lógica comercial

1. En cada vela M1 terminada, la estrategia actualiza el historial del patrón de precios, gestiona las posiciones existentes y evalúa si se permite la negociación (no se puede negociar el lunes antes de las 02:00 ni el viernes a partir de las 18:00, hora local de la plataforma).
2. Los valores de AO por hora se agregan en cinco perceptrones:
   - `Perceptron X/Y`: filtros BTS básicos que funcionan junto con el perceptrón de precios y el delta H4 AO.
   - `Neuro X/Y`: filtros avanzados largos/cortos que se utilizan cuando el modo neuro les otorga prioridad.
   - `Neuro Z`: perceptrón de activación que habilita Neuro X en modo 4.
3. El perceptrón de precios evalúa las diferencias ponderadas entre las recientes aperturas de M1 y el último cierre.
4. El parámetro **neuro mode** controla cómo intervienen los perceptrones en mayúsculas:
   - `4`: Si Neuro Z > 0, solo Neuro X puede generar una señal larga; de lo contrario, Neuro Y puede provocar un cortocircuito. Si ninguno de los dos dispara, recurre a BTS.
   - `3`: Neuro Y puede provocar cortos; de lo contrario, recurra a BTS.
   - `2`: Neuro X puede activar posiciones largas; de lo contrario, recurra a BTS.
   - Cualquier otro valor omite la capa neurológica y evalúa directamente BTS.
5. El bloque **BTS** utiliza el perceptrón de precios y el delta H4 AO como puertas:
   - Configuración larga: perceptrón de precios > 0 (a menos que `BtsMode = 0`), Neuro/BTS X > 0 y H4 AO delta > 0. El límite de pérdidas es `BaseStopLossPointsLong`, la obtención de ganancias es `BaseTakeProfitFactorLong × BaseStopLossPointsLong`.
   - Configuración corta: perceptrón de precios < 0 (a menos que `BtsMode = 0`), Neuro/BTS Y > 0 y H4 AO delta < 0. El límite de pérdidas es `BaseStopLossPointsShort`, la obtención de ganancias es `BaseTakeProfitFactorShort × BaseStopLossPointsShort`.
6. Después de que se acepta una señal, la estrategia abre una orden de mercado (respetando la dirección habilitada). Los precios de protección se rastrean internamente; Cada vela M1 terminada verifica si se alcanzó el stop o el objetivo usando máximos/mínimos de la vela y cierra la posición cuando sea apropiado. Las señales opuestas primero cierran la posición existente y esperan la siguiente vela antes de volver a entrar.

## Parámetros

### Comercio
- **OrderVolume**: volumen base para todas las órdenes del mercado.
- **AllowLongTrades / AllowShortTrades**: habilita o deshabilita entradas largas o cortas.
- **BtsMode**: cuando se establece en `0`, se ignora la puerta del perceptrón de precios en BTS; de lo contrario su signo debe alinearse con el oficio.
- **NeuroMode**: selecciona cómo participan los perceptrones avanzados (consulte la sección de lógica).

### Perceptrones base BTS
- **BaseStopLossPointsLong / BaseTakeProfitFactorLong** – Distancia de parada (puntos) y multiplicador para toma de ganancias larga.
- **BaseStopLossPointsShort / BaseTakeProfitFactorShort**: configuraciones análogas para operaciones cortas.
- **PerceptronPeriodX / Y** – Desplazamiento AO (en barras H1) utilizado por el perceptrón respectivo.
- **PerceptronWeightX1..4 / Y1..4** – Pesos (0–100) de las entradas del perceptrón; internamente se centran restando 50.
- **PerceptronThresholdX / Y**: salida mínima absoluta del perceptrón requerida antes de que se considere válida.

### Filtro de precios
- **PricePatternPeriod**: número de velas M1 que forman cada retraso en el perceptrón de precios.
- **PriceWeight1..4**: ponderaciones (centradas en 50) aplicadas a las diferencias de precios dentro del perceptrón.

### Neuroperceptrones
- **NeuroStopLossPointsLong / NeuroTakeProfitFactorLong** – Multiplicador de parada y TP utilizado por las señales de Neuro X.
- **NeuroStopLossPointsShort / NeuroTakeProfitFactorShort**: multiplicador de parada y TP utilizado por las señales de Neuro Y.
- **NeuroPeriodX / Y / Z** – Cambio AO (velas H1) para los tres neuroperceptrones.
- **NeuroWeightX1..4 / NeuroWeightY1..4 / NeuroWeightZ1..4** – Pesos del perceptrón.
- **NeuroThresholdX / NeuroThresholdY / NeuroThresholdZ** – Valor absoluto mínimo para cada neuroperceptrón.

### Datos
- **CandleType**: período de tiempo utilizado para las velas comerciales principales (predeterminado 1 minuto).

## Gestión comercial

- Las distancias de stop-loss y take-profit se convierten de puntos a precios absolutos utilizando el paso del precio del instrumento. Si una distancia se establece en cero, la protección correspondiente se desactiva.
- Los niveles de protección se monitorean en las velas M1 completadas comparando los máximos y mínimos de las velas con los precios almacenados.
- La estrategia funciona en modo de compensación: nunca mantiene simultáneamente posiciones largas y cortas. Una señal opuesta cierra primero la posición actual.

## Notas sobre la conversión

- Los enlaces de alto nivel StockSharp (`SubscribeCandles().Bind(...)`) se utilizan para transmitir valores de AO sin consultas directas de indicadores.
- Los buffers históricos se mantienen como listas de tamaño fijo para emular la indexación original basada en turnos y al mismo tiempo evitar búsquedas directas de indicadores.
- No se proporciona ninguna versión de Python, según lo solicitado.
- Las pruebas no fueron modificadas.
