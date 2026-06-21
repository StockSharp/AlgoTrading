# Estratégia IU de Rompimento do Intervalo de Abertura
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia IU Opening Range Breakout monitora a máxima e mínima da primeira barra de cada sessão e opera rompimentos em qualquer direção. Os stops usam o extremo da barra anterior e os alvos são derivados de uma relação risco/recompensa configurável. Todas as posições são fechadas em um horário de término definido pelo usuário.

## Detalhes

- **Critérios de entrada**:
  - Entrar comprado quando o fechamento cruza acima da máxima da primeira barra.
  - Entrar vendido quando o fechamento cruza abaixo da mínima da primeira barra.
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - Stop na mínima/máxima da barra anterior.
  - Alvo baseado na relação risco/recompensa.
  - Fechar todas as posições em `EndTime`.
- **Stops**: Sim
- **Valores padrão**:
  - `RiskReward` = 2.0
  - `MaxTrades` = 2
  - `EndTime` = 15:00
  - `CandleType` = 1 minuto
- **Filtros**:
  - Categoria: Rompimento
  - Direção: Ambos
  - Indicadores: Nenhum
  - Stops: Sim
  - Complexidade: Baixo
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
