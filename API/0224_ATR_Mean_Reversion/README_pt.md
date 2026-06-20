# Estratégia de Reversão à Média com ATR
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia de Reversão à Média com ATR mede o quanto o preço se distancia de uma média móvel em relação à volatilidade recente. O ATR fornece uma medida adaptativa para que os limiares se expandam durante períodos ativos e se contraiam quando os mercados ficam calmos.

Os testes indicam um retorno anual médio de aproximadamente 109%. Funciona melhor no mercado de criptomoedas.

Uma configuração comprada ocorre quando o preço fecha abaixo da média móvel em mais de `Multiplier` vezes o ATR. Uma configuração vendida aparece quando o preço fecha acima da média móvel pela mesma distância. As posições são encerradas assim que o preço retorna à média móvel.

Esta técnica é destinada a traders de curto prazo que esperam que os preços revertam após movimentos excessivos. O stop baseado em ATR mantém o risco proporcional às condições atuais do mercado.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Fechamento < MA - Multiplier * ATR
  - **Vendido**: Fechamento > MA + Multiplier * ATR
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando fechamento >= MA
  - **Vendido**: Sair quando fechamento <= MA
- **Stops**: Sim, stop-loss em torno de `2*ATR` por padrão.
- **Valores padrão**:
  - `MaPeriod` = 20
  - `AtrPeriod` = 14
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: MA, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
