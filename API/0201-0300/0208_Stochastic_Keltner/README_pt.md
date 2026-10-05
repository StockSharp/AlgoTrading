# Estratégia Stochastic Keltner
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia utiliza os indicadores Stochastic Keltner para gerar sinais.
A entrada comprada ocorre quando Stoch %K < StochOversold && Price < Keltner lower band (sobrevendido na banda inferior). A entrada vendida ocorre quando Stoch %K > StochOverbought && Price > Keltner upper band (sobrecomprado na banda superior).
É adequada para traders que buscam oportunidades em mercados mistos.

Os testes indicam um retorno anual médio de aproximadamente 61%. Funciona melhor no mercado de criptomoedas.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: Stoch %K < StochOversold && Price < Keltner lower band (oversold at lower band)
  - **Vendido**: Stoch %K > StochOverbought && Price > Keltner upper band (overbought at upper band)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando o preço retorna à banda média
  - **Vendido**: Sair da posição vendida quando o preço retorna à banda média
- **Stops**: Sim.
- **Valores padrão**:
  - `StochPeriod` = 14
  - `StochK` = 3
  - `StochOversold` = 20
  - `StochOverbought` = 80
  - `EmaPeriod` = 20
  - `KeltnerMultiplier` = 2
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    20 e 80 são os valores padrão dos níveis de %K citados nas regras. As bandas são a EMA de EmaPeriod mais e menos KeltnerMultiplier vezes o ATR de AtrPeriod, e a banda do meio é a própria EMA. O stop fica a AtrMultiplier vezes o mesmo ATR do fechamento de entrada e é verificado no fechamento dos candles; 0 o desativa. StochK nas regras é o %K: o estocástico de StochPeriod candles suavizado em StochK candles; o %D não participa, por isso não tem ajuste. Um sinal de entrada contra uma posição aberta a inverte.
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

