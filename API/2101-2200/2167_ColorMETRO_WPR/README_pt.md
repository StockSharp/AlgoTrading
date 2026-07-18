# Estratégia ColorMETRO WPR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia utiliza o indicador ColorMETRO Williams %R, que constrói linhas escalonadas rápidas e lentas em torno do oscilador Williams %R.
A linha rápida reage rapidamente às mudanças de preço, enquanto a linha lenta suaviza o ruído. As decisões de trading são tomadas quando essas linhas
se cruzam, sinalizando possíveis mudanças no impulso. Quando a linha rápida cruza abaixo da linha lenta, a estratégia assume que o
mercado está sobrevendido e entra em posição comprada. Por outro lado, quando a linha rápida cruza acima da linha lenta, entra em posição vendida.
As posições existentes são encerradas quando a condição oposta é detectada.

O gerenciamento de risco é feito por meio de níveis de take-profit e stop-loss baseados em percentual. Isso permite que a estratégia se adapte aos níveis de preço
em diferentes instrumentos. O período de tempo de velas padrão é de oito horas, o que ajuda a filtrar a volatilidade intradiária e
focar nas tendências de médio prazo. A lógica funciona em ambos os lados do mercado, habilitando operações compradas e vendidas.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: a `Linha rápida` cruza **abaixo** da `Linha lenta`.
  - **Vendido**: a `Linha rápida` cruza **acima** da `Linha lenta`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: a `Linha rápida` sobe acima da `Linha lenta`.
  - **Vendido**: a `Linha rápida` cai abaixo da `Linha lenta`.
- **Stops**: Sim, take-profit e stop-loss baseados em percentual.
- **Valores padrão**:
  - `WprPeriod` = 7.
  - `FastStep` = 5.
  - `SlowStep` = 15.
  - `TakeProfitPercent` = 4.
  - `StopLossPercent` = 2.
  - `CandleType` = velas de 8 horas.
- **Filtros**:
  - Categoria: Seguidor de tendência.
  - Direção: Ambos.
  - Indicadores: Único (baseado em Williams %R).
  - Stops: Sim.
  - Complexidade: Médio.
  - Período: Médio prazo.
  - Sazonalidade: Não.
  - Redes neurais: Não.
  - Divergência: Não.
  - Nível de risco: Médio.
