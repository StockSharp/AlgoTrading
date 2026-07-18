# Estratégia RedK de Média Lenta e Suave RSS WMA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Usa uma média móvel ponderada de tripla passagem para filtrar o ruído. Uma posição é aberta quando a média suavizada muda de direção: comprado quando vira para cima, vendido quando vira para baixo.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: a inclinação do WMA triplo vira para cima.
  - **Vendido**: a inclinação do WMA triplo vira para baixo.
- **Comprado/Vendido**: Ambos.
- **Critérios de saída**: sinal oposto.
- **Stops**: Não.
- **Valores padrão**:
  - `CombinedSmoothness` = 15
  - `CandleType` = 1 minute
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: WeightedMovingAverage
  - Stops: Não
  - Complexidade: Básico
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
