# Diagrama de la estrategia de ruptura por delta acumulado
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

El diagrama construye un delta de volumen direccional con velas terminadas de un minuto. Sum(100) móvil aporta la medida de ruptura, SMA(20) filtra las entradas y el umbral de delta opuesto cierra la posición abierta.

![schema](schema.svg)

## Resumen de la estrategia

- Cada vela terminada se separa en valores Open, Close y TotalVolume.
- Las velas alcistas y sin cambio aportan volumen positivo, mientras que las bajistas aportan volumen negativo.
- Sum(100) agrega los cien valores de volumen direccional más recientes y SMA(20) sigue los cierres; ambos indicadores solo emiten valores formados.
- Las comprobaciones de posición permiten entrar solo estando plana y envían un evento de delta opuesto a la salida reductora correspondiente.
- Las cuatro acciones son operaciones a mercado de una unidad y Strategy trades envía cada ejecución al gráfico.

## Reglas de entrada y salida

- **Entrada en largo**: Cuando el delta móvil es al menos +2, Close está sobre SMA(20) y la posición está plana, comprar una unidad a mercado.
- **Entrada en corto**: Cuando el delta móvil es como máximo -2, Close está bajo SMA(20) y la posición está plana, vender una unidad a mercado.
- **Salida**: Reducir un largo en una unidad cuando el delta llega a -2 o menos, y reducir un corto en una unidad cuando llega a +2 o más. Las puertas de salida no usan la SMA. El diagrama no tiene stop-loss ni take-profit.

## Parámetros

| Parámetro | Por defecto | Descripción |
|---|---|---|
| Candles Series | 00:01:00 | Velas terminadas de un minuto usadas en todos los cálculos y decisiones. |
| Delta Sum Length | 100 | Cantidad de valores de volumen direccional conservados por el indicador Sum móvil. |
| Delta Sum Source | Not set | No se selecciona otro campo de entrada del indicador; la fórmula de volumen direccional está conectada directamente. |
| SMA Length | 20 | Cantidad de cierres en la media móvil que filtra las entradas. |
| SMA Source | Not set | No se selecciona otro campo de entrada del indicador; Candle close está conectado directamente. |
| Delta Threshold | 2 | Nivel absoluto de delta usado como +2 para eventos alcistas y -2 para eventos bajistas. |
| Order Volume | 1 | Volumen de mercado fijo para entradas y salidas reductoras. |

## Detalles del diagrama

- La fórmula de volumen direccional es positiva cuando Close es mayor o igual que Open, por lo que un doji aporta +TotalVolume; el signo solo cambia si Close está bajo Open.
- El delta es una suma móvil de cien velas: una vez llena la ventana, cada valor nuevo reemplaza al más antiguo.
- Los umbrales positivo y negativo proceden de un solo valor expuesto, y una fórmula aplica el signo negativo a la rama bajista.
- El pulso final de evaluación llega a las cuatro puertas AND después de actualizar campos de vela, indicadores, comparaciones y la instantánea de posición.
- No hay un periodo de espera por número de velas. Las puertas de posición plana, larga y corta impiden añadir y seleccionan la acción válida para cada vela.
- El gráfico recibe seis flujos: velas, delta móvil, umbral positivo, umbral negativo, SMA(20) y todas las ejecuciones de entrada y salida.

## Uso

Importe el archivo `.json` en Designer, ejecútelo sobre datos históricos en el probador y después ajuste los parámetros o los propios bloques a su instrumento antes de operar en real.
