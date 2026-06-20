# Keltner Williams R Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia usa os indicadores Keltner Williams R para gerar sinais.
A entrada comprada ocorre quando Price < lower Keltner band && Williams %R < -80 (sobrevendido na banda inferior). A entrada vendida ocorre quando Price > upper Keltner band && Williams %R > -20 (sobrecomprado na banda superior).
É adequada para traders que buscam oportunidades em mercados mistos.

Os testes indicam um retorno anual médio de aproximadamente 46%. Funciona melhor no mercado de ações.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Price < lower Keltner band && Williams %R < -80 (sobrevendido na banda inferior)
  - **Vendido**: Price > upper Keltner band && Williams %R > -20 (sobrecomprado na banda superior)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando o preço retorna à banda do meio
  - **Vendido**: Sair da posição vendida quando o preço retorna à banda do meio
- **Stops**: Sim.
- **Valores padrão**:
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `WilliamsRPeriod` = 14
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: Keltner Williams R
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

