# Estrategia de Day Trading PAMXA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción
La estrategia **Day Trading PAMXA** reproduce el asesor experto de MetaTrader 5 que combina reversiones de momentum del Awesome Oscillator de Bill Williams con un filtro estocástico. El port de StockSharp conserva el diseño multitemporal original:

- El bucle de decisión principal se ejecuta en el marco temporal de **Velas de señal** (por defecto 1 hora).
- El Awesome Oscillator se evalúa en un marco temporal separado de **Velas AO** (por defecto 1 día) para obtener momentum de marco temporal superior.
- El oscilador estocástico utiliza su propio marco temporal de **Velas estocásticas** (por defecto 1 hora) para que los niveles %K/%D estén alineados con los ajustes originales.

La estrategia mantiene como máximo una posición a la vez. Cuando aparece una configuración alcista, primero cubre cualquier corto activo antes de entrar en largo, y viceversa para configuraciones bajistas.

## Lógica de entrada
1. Calcular los valores de Awesome Oscillator más recientes terminados en el marco temporal AO.
2. Calcular los valores más recientes terminados de %K y %D del oscilador estocástico en el marco temporal estocástico.
3. En cada vela de señal terminada:
   - **Configuración alcista**: se activa cuando la barra AO anterior estaba por debajo de cero y la última barra cerró por encima de cero (reversión de momentum) mientras %K o %D está por debajo del umbral `Nivel estocástico bajo` (condición de sobreventa). Cualquier corto abierto se cubre y se abre un nuevo largo si no queda posición.
   - **Configuración bajista**: se activa cuando la barra AO anterior estaba por encima de cero y la última barra cerró por debajo de cero mientras %K o %D está por encima del umbral `Nivel estocástico alto` (condición de sobrecompra). Cualquier largo abierto se cierra y, si está plano, se abre una nueva posición corta.

## Salida y gestión de riesgo
- Un **stop-loss** y **take-profit** basados en pips se adjuntan en la entrada. Cuando el mínimo de la vela (para largos) o el máximo (para cortos) supera el nivel de stop, la posición se liquida inmediatamente. La misma lógica aplica al objetivo de beneficio.
- Un **trailing stop** opcional se activa cuando el precio se ha movido `Trailing Stop + Trailing Step` pips a favor de la posición. Para largos el stop sigue el máximo más alto menos la distancia de trailing; para cortos sigue el mínimo más bajo más la distancia de trailing. El ajuste del trailing ocurre solo cuando el movimiento supera el paso de trailing, replicando el comportamiento original del EA.
- La gestión monetaria puede operar en dos modos:
  - `FixedVolume`: usa el parámetro `Order Volume` directamente.
  - `RiskPercent`: calcula el volumen de modo que el porcentaje configurado del valor del portafolio se perdería si se alcanza el stop-loss. El cálculo usa la distancia de stop en pips y redondea al paso de volumen más cercano.
- La estrategia nunca hace pirámide – una vez que existe una posición, la próxima señal opuesta la aplanará antes de considerar cualquier nueva entrada.

## Parámetros
| Parámetro | Descripción |
|-----------|-------------|
| `Stop Loss` | Distancia de stop-loss en pips. Cero desactiva el stop protector.
| `Take Profit` | Distancia de take-profit en pips. Cero desactiva el objetivo de beneficio.
| `Trailing Stop` | Distancia de activación del trailing stop en pips. Cero desactiva el trailing.
| `Trailing Step` | Pips adicionales requeridos antes de que el trailing stop avance. Debe ser positivo cuando el trailing está activado.
| `Money Mode` | Selecciona entre dimensionamiento `FixedVolume` y `RiskPercent`.
| `Money Value` | Interpretado como tamaño de lote con volumen fijo, o como porcentaje de riesgo con dimensionamiento basado en riesgo.
| `Order Volume` | Volumen base usado cuando `Money Mode` es `FixedVolume`.
| `Stochastic %K` | Longitud del periodo de cálculo estocástico %K.
| `Stochastic %D` | Longitud de suavizado para la línea estocástica %D.
| `Stochastic Slow` | Factor de suavizado final aplicado al oscilador estocástico.
| `Level Up` | Umbral estocástico superior que habilita entradas cortas.
| `Level Down` | Umbral estocástico inferior que habilita entradas largas.
| `Signal Candles` | Marco temporal que impulsa el bucle principal de trading.
| `Stochastic Candles` | Marco temporal que alimenta el oscilador estocástico.
| `AO Candles` | Marco temporal que alimenta el Awesome Oscillator.
| `AO Fast` / `AO Slow` | Períodos para las medias móviles internas del Awesome Oscillator.

## Notas de implementación
- El cálculo del valor del pip emula la lógica de MetaTrader: cuando el instrumento usa 3 o 5 decimales, un pip equivale a diez pasos de precio; de lo contrario equivale a un paso de precio.
- El oscilador estocástico de StockSharp no expone una selección dedicada de "campo de precio"; el port usa el cálculo basado en cierre predeterminado mientras conserva los parámetros de suavizado configurables.
- El manejo del trailing stop se implementa como una verificación virtual en los máximos/mínimos de las velas. Esto replica los ajustes de stop del lado servidor realizados en MetaTrader sin registrar órdenes de stop explícitas.
- El código se suscribe a todos los marcos temporales de velas requeridos a través de `GetWorkingSecurities`, permitiendo al motor solicitar datos para los marcos temporales de señal, estocástico y AO concurrentemente.
- Los comentarios en inglés documentan las decisiones de flujo de control más importantes para un mantenimiento más fácil.

## Consejos de uso
- Alinee el marco temporal de `Signal Candles` con el marco temporal en el que planea hacer backtesting o trading. Mantenga `Stochastic Candles` y `AO Candles` iguales a los valores predeterminados originales cuando quiera replicar exactamente el experto MQL5.
- Al cambiar a dimensionamiento `RiskPercent`, asegúrese de que la distancia de stop-loss sea distinta de cero; de lo contrario la estrategia cae de vuelta a `Order Volume`.
- La configuración de trailing predeterminada refleja el EA original (trailing stop de 25 pips con paso de 5 pips). Establezca `Trailing Stop` en cero si prefiere un stop-loss estático.
