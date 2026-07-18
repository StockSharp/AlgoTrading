# Estratégia de Rompimento NY
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Opera Rompimentos do range formado entre 13:00 e 13:30 UTC. Após o fechamento da janela, a estratégia entra quando o preço rompe a máxima ou mínima da sessão, mirando o dobro do range e colocando o stop no lado oposto.

## Detalhes

- **Critérios de entrada**:
  - Primeira vela após 13:30 UTC fecha acima da máxima da sessão -> comprado.
  - Primeira vela após 13:30 UTC fecha abaixo da mínima da sessão -> vendido.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Alvo de lucro em `RewardRisk` vezes o range.
  - Stop no limite oposto do range.
- **Stops**: Sim.
- **Valores padrão**:
  - `RewardRisk` = 2
  - `CandleType` = TimeSpan.FromMinutes(1)
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Básico
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
