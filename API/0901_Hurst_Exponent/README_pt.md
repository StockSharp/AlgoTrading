# Hurst Exponent
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia simples que opera com base em um Hurst Exponent suavizado.  
O valor de Hurst é suavizado com uma EMA e comparado a um limiar para determinar o regime de mercado.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Hurst suavizado > Limiar
  - **Vendido**: Hurst suavizado < Limiar
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Hurst suavizado < Limiar
  - **Vendido**: Hurst suavizado > Limiar
- **Stops**: Sim, stop-loss percentual.
- **Valores padrão**:
  - `HurstPeriod = 100`
  - `SmoothLength = 10`
  - `Threshold = 0.5m`
  - `CandleType = TimeSpan.FromMinutes(5)`
- **Filtros**:
  - Categoria: Tendência
  - Direção: Ambos
  - Indicadores: Hurst Exponent, EMA
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
