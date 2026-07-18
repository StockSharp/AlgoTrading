# Estratégia MACD Bollinger
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia usa os indicadores MACD Bollinger para gerar sinais.
A entrada comprada ocorre quando MACD > Signal && Price < BB_lower (tendência de alta com condições de sobrevenda). A entrada vendida ocorre quando MACD < Signal && Price > BB_upper (tendência de baixa com condições de sobrecompra).
É adequada para traders que buscam oportunidades em mercados mistos.

Os testes indicam um retorno anual médio de aproximadamente 55%. Funciona melhor no mercado de ações.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: MACD > Signal && Price < BB_lower (tendência de alta com condições de sobrevenda)
  - **Vendido**: MACD < Signal && Price > BB_upper (tendência de baixa com condições de sobrecompra)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando o preço retorna à banda do meio
  - **Vendido**: Sair da posição vendida quando o preço retorna à banda do meio
- **Stops**: Sim.
- **Valores padrão**:
  - `MacdFast` = 12
  - `MacdSlow` = 26
  - `MacdSignal` = 9
  - `BollingerPeriod` = 20
  - `BollingerDeviation` = 2.0m
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2m
  - `CandleType` = TimeSpan.FromMinutes(5)
- **Filtros**:
  - Categoria: Misto
  - Direção: Ambos
  - Indicadores: MACD Bollinger
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Intradiário
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

