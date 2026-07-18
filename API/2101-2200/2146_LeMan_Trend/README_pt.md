# Estratégia LeMan Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia LeMan Tendência deriva pressão de alta e de baixa a partir das máximas e mínimas recentes. Calcula a distância entre a vela atual e as máximas mais altas e as mínimas mais baixas ao longo de três períodos de retrovisão diferentes. Essas distâncias são suavizadas com uma média móvel exponencial (EMA) para formar duas linhas: touros e ursos. Um cruzamento entre essas linhas sinaliza possíveis mudanças de tendência.

Quando a linha dos touros cruza acima da linha dos ursos, a estratégia abre uma posição comprada ou fecha uma posição vendida existente. Por outro lado, quando a linha dos ursos se move acima da linha dos touros, ela abre uma posição vendida ou sai de uma comprada. O método não usa filtros adicionais, focando apenas na força relativa das máximas e mínimas recentes.

## Detalhes

- **Critérios de entrada**
  - **Comprado**: A linha dos touros cruza acima da linha dos ursos.
  - **Vendido**: A linha dos ursos cruza acima da linha dos touros.
- **Comprado/Vendido**: Ambos os lados suportados.
- **Critérios de saída**
  - O cruzamento oposto fecha a posição ativa.
- **Stops**: Nenhum por padrão.
- **Valores padrão**
  - `Min` = 13
  - `Midle` = 21
  - `Max` = 34
  - `EMA period` = 3
  - `Time frame` = 4 hours
- **Filtros**
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Highest, Lowest, EMA
  - Stops: Não
  - Complexidade: Moderado
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Moderado
