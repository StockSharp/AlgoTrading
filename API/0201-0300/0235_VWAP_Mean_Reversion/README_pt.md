# Estratégia de Reversão à Média com VWAP
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia opera contra movimentos que se afastam do preço médio ponderado por volume. O ATR é usado para medir o quanto o preço deve se desviar do VWAP antes de considerar uma operação de reversão.

Os testes indicam um retorno anual médio de aproximadamente 58%. Funciona melhor no mercado de ações.

Uma posição comprada é aberta quando o preço cai abaixo do VWAP em mais de `K` vezes o ATR. Uma posição vendida é tomada quando o preço sobe acima do VWAP pela mesma quantidade. As operações são encerradas assim que o preço retorna à linha VWAP.

A abordagem é projetada para traders intradiários que esperam que os preços oscilem em torno do VWAP em vez de tendências fortes. Stops dimensionados como múltiplo do ATR ajudam a manter as perdas controladas se o movimento continuar contra a operação.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Close < VWAP - K * ATR
  - **Vendido**: Close > VWAP + K * ATR
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair quando close >= VWAP
  - **Vendido**: Sair quando close <= VWAP
- **Stops**: Sim, stop baseado em ATR.
- **Valores padrão**:
  - `K` = 2.0m
  - `CandleType` = TimeSpan.FromMinutes(5)
  - `AtrPeriod` = 14
- **Filtros**:
  - Categoria: Mean reversion
  - Direção: Ambos
  - Indicadores: VWAP, ATR
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

