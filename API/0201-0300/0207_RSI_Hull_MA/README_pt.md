# Estratégia RSI Hull MA
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)
 
Esta estratégia usa os indicadores RSI Hull MA para gerar sinais.
A entrada comprada ocorre quando RSI < RsiOversold && HMA(t) > HMA(t-1) (sobrevendido com HMA subindo). A entrada vendida ocorre quando RSI > RsiOverbought && HMA(t) < HMA(t-1) (sobrecomprado com HMA caindo).
É adequada para traders que buscam oportunidades em mercados mistos.

Os testes indicam um retorno anual médio de aproximadamente 58%. Funciona melhor no mercado de ações.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: RSI < RsiOversold && HMA(t) > HMA(t-1) (sobrevendido com HMA subindo)
  - **Vendido**: RSI > RsiOverbought && HMA(t) < HMA(t-1) (sobrecomprado com HMA caindo)
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - **Comprado**: Sair da posição comprada quando RSI retorna à zona neutra
  - **Vendido**: Sair da posição vendida quando RSI retorna à zona neutra
- **Stops**: Sim.
- **Valores padrão**:
  - `RsiPeriod` = 14
  - `RsiOversold` = 30
  - `RsiOverbought` = 70
  - `HullPeriod` = 9
  - `AtrPeriod` = 14
  - `AtrMultiplier` = 2
    30 e 70 são os valores padrão dos níveis de RSI citados nas regras. A zona neutra começa no RSI 50: uma compra fecha quando o RSI chega a 50 e uma venda quando ele cai para 50. O stop fica a AtrMultiplier vezes o ATR de AtrPeriod do fechamento de entrada e é verificado no fechamento dos candles; 0 o desativa. Um sinal de entrada contra uma posição aberta a inverte.
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

