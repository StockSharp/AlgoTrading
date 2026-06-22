# Estratégia NonLagDot
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Estratégia inspirada no indicador NonLagDot. O indicador aproxima a tendência do preço usando uma média móvel suavizada e pontos codificados por cores.
A estratégia abre uma posição comprada quando o indicador vira para cima e uma posição vendida quando vira para baixo.
Posições opostas anteriores são fechadas antes de abrir uma nova.

## Detalhes

- **Critérios de entrada**:
  - Comprado: o indicador passa de descendente para ascendente (a inclinação da média móvel torna-se positiva)
  - Vendido: o indicador passa de ascendente para descendente (a inclinação torna-se negativa)
- **Comprado/Vendido**: Ambos
- **Critérios de saída**: sinal oposto
- **Stops**: percentual de stop-loss opcional
- **Valores padrão**:
  - `Length` = 10
  - `CandleType` = TimeSpan.FromHours(1).TimeFrame()
  - `StopLossPercent` = 1m
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: aproximação da inclinação SMA do NonLagDot
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
