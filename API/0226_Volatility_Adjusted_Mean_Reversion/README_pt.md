# Estratégia de Reversão à Média Ajustada pela Volatilidade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta variação da reversão à média escala os limiares de entrada pela razão entre ATR e desvio padrão. Quando a volatilidade aumenta em relação ao ruído típico, a distância necessária para acionar uma operação cresce, ajudando a evitar sinais prematuros durante oscilações caóticas.

Os testes indicam um retorno anual médio de aproximadamente 115%. Funciona melhor no mercado de ações.

Uma posição comprada é aberta quando o preço cai abaixo da média móvel em mais do limiar ajustado. Uma posição vendida é aberta quando o preço sobe acima da média pela mesma medida. As posições são encerradas assim que o preço fecha de volta próximo ao nível médio.

O limiar adaptativo torna esta estratégia adequada para mercados com regimes de volatilidade em mudança. Um stop-loss igual ao dobro do ATR limita o risco enquanto se aguarda a reversão.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Fechamento < MA - Multiplier * ATR / (ATR/StdDev)
  - **Vendido**: Fechamento > MA + Multiplier * ATR / (ATR/StdDev)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando fechamento >= MA
  - **Vendido**: Sair quando fechamento <= MA
- **Stops**: Sim, dinâmico baseado em ATR.
- **Valores padrão**:
  - `Period` = 20
  - `Multiplier` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: ATR, StdDev
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
