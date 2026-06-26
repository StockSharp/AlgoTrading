# Estrategia Perceptron Mult
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia porta el experto **Peceptron_Mult.mq5** a la API de alto nivel de StockSharp. Monitorea simultáneamente hasta tres mercados independientes y aplica el oscilador Acceleration/Deceleration (AC) dentro de un modelo de perceptrón. Cada mercado recibe su propia configuración de pesos, dimensionamiento de posición y salidas de protección, de modo que se preserve el comportamiento del asesor original multi-símbolo.

## Lógica de Trading

1. Para cada instrumento configurado, la estrategia se suscribe al mismo tipo de vela (predeterminado: 1 minuto).
2. En cada vela terminada calcula el oscilador Acceleration/Deceleration de Bill Williams:
   - Calcular el Awesome Oscillator (AO) a partir de los máximos y mínimos de la vela (medias móviles de precio mediano 5/34).
   - Restar una media móvil simple de 5 períodos de AO del valor actual de AO.
3. Se mantiene un buffer circular con los últimos 22 valores de AC por instrumento.
4. La señal del perceptrón se forma a partir de cuatro valores retardados de AC usando pesos (`w - 100`) exactamente como en el código MQL:
   - `AC[0]`, `AC[7]`, `AC[14]`, `AC[21]` corresponden a la lectura más reciente y tres históricas.
5. Reglas de entrada:
   - Suma positiva ⇒ abrir posición larga si no existe posición en ese instrumento.
   - Suma negativa ⇒ abrir posición corta si el instrumento está plano.
6. Reglas de salida:
   - Las distancias de stop-loss y take-profit se expresan en puntos. Se convierten a desplazamientos de precio absolutos usando el paso de precio del instrumento.
   - Las salidas de protección se evalúan en cada vela terminada. Una operación larga se cierra cuando el mínimo de la vela toca el stop o el máximo alcanza el objetivo de ganancia; los cortos usan la lógica espejada.
7. Las posiciones son mutuamente exclusivas por instrumento. La estrategia ignora nuevas señales mientras la exposición permanece abierta, replicando el comportamiento del asesor original.

## Parámetros

| Parámetro | Descripción |
| --- | --- |
| `FirstSecurity`, `SecondSecurity`, `ThirdSecurity` | Instrumentos procesados por el perceptrón. Dejar en `null` para deshabilitar un slot.
| `FirstOrderVolume`, `SecondOrderVolume`, `ThirdOrderVolume` | Tamaño de orden a mercado para cada instrumento.
| `FirstWeight1`…`FirstWeight4`, etc. | Pesos del perceptrón (entradas MQL `x1…x12`). La estrategia resta internamente 100 de cada valor antes de aplicarlo.
| `FirstStopLossPoints`, `SecondStopLossPoints`, `ThirdStopLossPoints` | Distancia de stop-loss en puntos de precio para cada instrumento. Establecer en 0 para deshabilitar.
| `FirstTakeProfitPoints`, `SecondTakeProfitPoints`, `ThirdTakeProfitPoints` | Distancia de take-profit en puntos de precio para cada instrumento. Establecer en 0 para deshabilitar.
| `CandleType` | Serie de velas compartida por todos los instrumentos.

## Notas de Implementación

- La estrategia utiliza los indicadores `AwesomeOscillator` y `SimpleMovingAverage` de StockSharp para reconstruir el oscilador AC, evitando recálculos manuales.
- Los buffers circulares se usan solo para emular las entradas del perceptrón de la implementación MQL (índices 0, 7, 14, 21).
- Los niveles de protección se aplican sin registrar órdenes stop separadas: la estrategia monitorea los extremos de las velas y cierra posiciones con órdenes a mercado cuando se violan los niveles, reflejando el comportamiento del EA original en nuevos ticks.
- Cada instrumento mantiene un estado de indicador independiente, volumen de orden y configuraciones de riesgo, coincidiendo con la estructura de tres símbolos del asesor fuente.

## Consejos de Uso

1. Asignar hasta tres instrumentos en el panel de parámetros. Cualquier slot no utilizado puede quedar como `null`.
2. Ajustar los stops y objetivos basados en puntos para que coincidan con el tamaño de tick de los instrumentos seleccionados.
3. Ajustar los pesos del perceptrón para enfatizar rezagos específicos del oscilador AC si se requiere optimización.
4. Dado que todos los instrumentos comparten el mismo tipo de vela, asegurarse de que los datos históricos estén disponibles para cada instrumento configurado.
