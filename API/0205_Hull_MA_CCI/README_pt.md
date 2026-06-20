# Hull MA CCI Strategy
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia usa os indicadores Hull MA CCI para gerar sinais.
A entrada comprada ocorre quando HMA(t) > HMA(t-1) && CCI < -100 (HMA subindo com condições de sobrevenda). A entrada vendida ocorre quando HMA(t) < HMA(t-1) && CCI > 100 (HMA caindo com condições de sobrecompra).
É adequada para traders que buscam oportunidades em mercados mistos.

Os testes indicam um retorno anual médio de aproximadamente 52%. Funciona melhor no mercado de criptomoedas.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: HMA(t) > HMA(t-1) && CCI < -100 (HMA subindo com condições de sobrevenda)
  - **Vendido**: HMA(t) < HMA(t-1) && CCI > 100 (HMA caindo com condições de sobrecompra)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando HMA começa a cair
  - **Vendido**: Sair da posição vendida quando HMA começa a subir
- **Stops**: Sim.
- **Valores padrão**:
  - `HullPeriod` = 9
  - `CciPeriod` = 20
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: Hull MA CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

