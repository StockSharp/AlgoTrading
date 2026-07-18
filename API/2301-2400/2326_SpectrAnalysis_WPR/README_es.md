# Estrategia SpectrAnalysis WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

Esta estrategia se convirtió a partir del experto MQL5 *Exp_i-SpectrAnalysis_WPR*.
Analiza la dirección del indicador Williams %R y abre o cierra posiciones según los giros del indicador.

## Lógica

1. Suscribirse a las velas del marco temporal seleccionado.
2. Calcular Williams %R con el período configurado.
3. Conservar los últimos dos valores del indicador para detectar dirección ascendente o descendente.
4. Cuando el indicador gira hacia arriba y las entradas largas están permitidas:
   - Cerrar posiciones cortas si está habilitado.
   - Abrir una nueva posición larga.
5. Cuando el indicador gira hacia abajo y las entradas cortas están permitidas:
   - Cerrar posiciones largas si está habilitado.
   - Abrir una nueva posición corta.

Solo se procesan velas completadas. La estrategia no utiliza consultas históricas complejas y se basa en enlaces de API de alto nivel.

## Parámetros

| Nombre | Descripción | Predeterminado |
| --- | --- | --- |
| `Candle Type` | Marco temporal de las velas utilizadas para los cálculos | `4h` |
| `WPR Period` | Período del indicador Williams %R | `13` |
| `Allow Long Entry` | Permitir abrir posiciones largas | `true` |
| `Allow Short Entry` | Permitir abrir posiciones cortas | `true` |
| `Allow Long Exit` | Permitir cerrar posiciones largas | `true` |
| `Allow Short Exit` | Permitir cerrar posiciones cortas | `true` |

## Notas

La versión MQL original aplicaba análisis espectral a la salida de Williams %R.
Esta conversión en C# usa el indicador Williams %R estándar y replica la lógica de señales rastreando los valores recientes del indicador.
