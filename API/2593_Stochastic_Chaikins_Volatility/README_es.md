# Estrategia de Volatilidad de Stochastic Chaikin's
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

## Descripción general
Esta estrategia es un port en StockSharp del asesor experto MetaTrader `Exp_Stochastic_Chaikins_Volatility`. Analiza la diferencia entre los precios máximo y mínimo, suaviza esa volatilidad con una media móvil configurable y luego normaliza el resultado usando un oscilador similar al estocástico. Las decisiones de trading siguen la lógica contraria a la tendencia original: la estrategia busca puntos de giro en el oscilador para desvanecerse los extremos a corto plazo mientras cierra opcionalmente las posiciones existentes cuando el impulso se invierte.

## Construcción del indicador
1. **Volatilidad estilo Chaikin** – la diferencia entre el máximo y el mínimo de la vela se suaviza con la media móvil *primaria*. Los métodos de suavizado admitidos son:
   - Simple (SMA)
   - Exponencial (EMA)
   - Suavizada/Wilder (SMMA)
   - Ponderada linealmente (LWMA)
   - Jurik (aproximación JMA)
2. **Normalización estocástica** – los `Stochastic Length` valores suavizados más recientes definen el rango más alto y más bajo. El valor suavizado actual se normaliza en un rango de 0–100 usando esa ventana.
3. **Suavizado secundario** – una segunda media móvil (método seleccionable de la misma lista) se aplica al valor normalizado para obtener la línea principal del oscilador. Internamente la línea de señal es simplemente el valor del oscilador de la vela completada anterior, replicando el comportamiento del buffer del indicador MQL.

## Lógica de trading
- **Entrada**
  - *Comprar*: cuando el oscilador principal formó un máximo más bajo (el valor anterior es mayor que su propio valor previo, el valor actual cruza por debajo de ese valor anterior). Esto refleja el disparador largo contrario del EA original.
  - *Vender*: cuando el oscilador formó un mínimo más alto (el valor anterior es menor que su propio valor previo, el valor actual cruza por encima de ese valor anterior).
- **Salida**
  - Las posiciones largas se cierran cuando el valor del oscilador anterior se mueve por debajo de su valor más antiguo (reaparece el impulso descendente).
  - Las posiciones cortas se cierran cuando el valor del oscilador anterior sube por encima de su valor más antiguo.
- La evaluación de señales usa el parámetro `Signal Shift` para inspeccionar velas completadas. Los valores predeterminados emulan la configuración MQL de 1 barra.

## Parámetros
| Nombre | Descripción |
| --- | --- |
| `Candle Type` | Marco temporal usado para todos los cálculos (por defecto velas temporales de 4 horas). |
| `Primary Method` / `Primary Length` | Tipo y longitud de media móvil para suavizar la diferencia máximo–mínimo. |
| `Secondary Method` / `Secondary Length` | Tipo y longitud de media móvil para suavizar el oscilador normalizado. |
| `Stochastic Length` | Ventana de retrospectiva para el rango más alto/más bajo usado en el paso de normalización. |
| `Signal Shift` | Número de velas completadas entre la barra actual y la barra usada para la evaluación de señal. Debe mantenerse ≥1. |
| `Allow Long/Short Entry` | Habilitar o deshabilitar la apertura de operaciones largas o cortas. |
| `Allow Long/Short Exit` | Habilitar o deshabilitar el cierre de posición cuando el oscilador se invierte. |
| `High/Middle/Low Level` | Niveles de guía visual reproducidos del indicador original (sin efecto directo en el trading). |

## Notas de uso
- El port de StockSharp mantiene el comportamiento contrario a la tendencia original pero usa las medias móviles de StockSharp. Los métodos exóticos de la biblioteca MQL (ParMA, VIDYA, AMA, etc.) se mapean a la opción de suavizado disponible más cercana; elija Jurik para una aproximación más cercana cuando sea necesario.
- El dimensionamiento de posición sigue la propiedad `Volume` de la estrategia base. La gestión de stop-loss y take-profit de la biblioteca auxiliar MQL no se replica; las salidas dependen de reversiones del oscilador o gestión de riesgo externa como `StartProtection`.
- Las señales se calculan solo en velas terminadas. Asegúrese de que el feed de datos proporcione el `Candle Type` seleccionado con suficiente historial para que ambas etapas de suavizado y la ventana estocástica puedan calentarse.
