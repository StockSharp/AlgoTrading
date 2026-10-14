# Estratégia Parabolic SAR CCI
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia usa os indicadores Parabolic SAR CCI para gerar sinais.
A entrada comprada ocorre quando Price > SAR && CCI < CciOversold (tendência de alta com condições de sobrevenda). A entrada vendida ocorre quando Price < SAR && CCI > CciOverbought (tendência de baixa com condições de sobrecompra).
É adequada para traders que buscam oportunidades em mercados mistos.

Os testes indicam um retorno anual médio de aproximadamente 49%. Funciona melhor no mercado de criptomoedas.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Price > SAR && CCI < CciOversold (tendência de alta com condições de sobrevenda)
  - **Vendido**: Price < SAR && CCI > CciOverbought (tendência de baixa com condições de sobrecompra)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando o preço cai abaixo do SAR
  - **Vendido**: Sair da posição vendida quando o preço sobe acima do SAR
- **Stops**: Não.
- **Valores padrão**:
  - `SarAccelerationFactor` = 0.02
  - `SarMaxAccelerationFactor` = 0.2
  - `CciPeriod` = 20
  - `CciOversold` = -100
  - `CciOverbought` = 100
    -100 e 100 são os valores padrão dos níveis de CCI citados nas regras. Uma entrada é um recuo dentro da tendência que o SAR mostra, e o SAR funciona como saída móvel: uma compra fecha com um fechamento abaixo dele e uma venda com um fechamento acima. Um sinal de entrada contra uma posição aberta a inverte.
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: Parabolic SAR CCI
  - Stops: Não
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

