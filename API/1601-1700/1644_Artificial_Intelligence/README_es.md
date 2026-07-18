# Estrategia de Inteligencia Artificial con Perceptrón
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Deutsch](README_de.md) | [Português](README_pt.md) | [日本語](README_ja.md)

La estrategia de Inteligencia Artificial utiliza un perceptrón simple para combinar múltiples lecturas del Oscillador Acelerador (AC) en diferentes desplazamientos temporales. La suma ponderada del valor actual de AC y tres valores rezagados (7, 14, 21 barras atrás) determina la dirección de la operación. Cuando la salida del perceptrón es positiva, la estrategia abre o mantiene una posición larga; cuando es negativa, abre o mantiene una posición corta.

Después de una entrada, la estrategia protege la operación con un stop-loss expresado en puntos. A medida que el precio se mueve en la dirección rentable, el nivel de stop sigue al precio. Si la salida del perceptrón cambia de signo mientras la posición es rentable, la estrategia revierte, cerrando la posición actual y entrando en la opuesta.

Las pruebas muestran que este enfoque puede reaccionar rápidamente a los cambios de momentum manteniendo el riesgo bajo control. Funciona en cualquier instrumento que proporcione datos de velas y no depende de regímenes de mercado específicos.

## Detalles

- **Criterios de entrada**  
  - **Largo**: Salida del perceptrón > 0 y sin posición larga existente.  
  - **Corto**: Salida del perceptrón < 0 y sin posición corta existente.
- **Salida / Reversión**  
  - Stop trailing activado.  
  - La salida del perceptrón cambia de signo; la estrategia revierte la posición.
- **Stops**: Sí, stop trailing basado en el parámetro `StopLoss`.
- **Valores predeterminados**  
  - `X1 = 135`  
  - `X2 = 127`  
  - `X3 = 16`  
  - `X4 = 93`  
  - `StopLoss = 85`
- **Filtros**  
  - Categoría: Momentum  
  - Dirección: Ambos  
  - Indicadores: Accelerator Oscillator  
  - Stops: Sí  
  - Complejidad: Medio  
  - Marco temporal: Corto plazo  
  - Redes neuronales: Perceptrón  
  - Divergencia: No  
  - Nivel de riesgo: Medio
