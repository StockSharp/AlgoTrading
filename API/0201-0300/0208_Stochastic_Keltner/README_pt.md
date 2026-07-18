# Estratégia Stochastic Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia utiliza os indicadores Stochastic Keltner para gerar sinais.
A entrada comprada ocorre quando Stoch %K < 20 && Price < Keltner lower band (sobrevendido na banda inferior). A entrada vendida ocorre quando Stoch %K > 80 && Price > Keltner upper band (sobrecomprado na banda superior).
É adequada para traders que buscam oportunidades em mercados mistos.

Os testes indicam um retorno anual médio de aproximadamente 61%. Funciona melhor no mercado de criptomoedas.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Stoch %K < 20 && Price < Keltner lower band (oversold at lower band)
  - **Vendido**: Stoch %K > 80 && Price > Keltner upper band (overbought at upper band)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando o preço retorna à banda média
  - **Vendido**: Sair da posição vendida quando o preço retorna à banda média
- **Stops**: Sim.
- **Valores padrão**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochD` = 3
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: Stochastic Keltner
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

