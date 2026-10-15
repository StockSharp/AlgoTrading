# Cruzamento de MA de Volume (Volume MA Cross)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia processa o volume por meio de médias móveis rápida e lenta. Quando a MA de volume rápida cruza acima da MA de volume lenta, indica maior participação e aciona uma entrada comprada. Um cruzamento abaixo sinaliza fraqueza e inicia uma posição vendida.

Os testes indicam um retorno anual médio de aproximadamente 46%. Funciona melhor no mercado de ações.

As posições são fechadas quando ocorre o cruzamento inverso. O preço é monitorado com sua própria média móvel para ajudar a filtrar as operações.

Sinais baseados em volume frequentemente precedem o movimento do preço, permitindo entradas antecipadas.

## Detalhes

- **Critérios de entrada**: A MA de volume rápida cruza a MA de volume lenta.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**: Cruzamento inverso ou stop.
- **Stops**: Sim.
- **Valores padrão**:
  - `FastVolumeMALength` = 10
  - `SlowVolumeMALength` = 50
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Momentum
  - Direção: Ambos
  - Indicadores: Volume MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
