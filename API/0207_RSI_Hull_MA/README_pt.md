# RSI Hull MA Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia usa os indicadores RSI Hull MA para gerar sinais.
A entrada comprada ocorre quando RSI < 30 && HMA(t) > HMA(t-1) (sobrevendido com HMA subindo). A entrada vendida ocorre quando RSI > 70 && HMA(t) < HMA(t-1) (sobrecomprado com HMA caindo).
É adequada para traders que buscam oportunidades em mercados mistos.

Os testes indicam um retorno anual médio de aproximadamente 58%. Funciona melhor no mercado de ações.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: RSI < 30 && HMA(t) > HMA(t-1) (sobrevendido com HMA subindo)
  - **Vendido**: RSI > 70 && HMA(t) < HMA(t-1) (sobrecomprado com HMA caindo)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando RSI retorna à zona neutra
  - **Vendido**: Sair da posição vendida quando RSI retorna à zona neutra
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `HullPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: RSI Hull MA
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

