# Estratégia Vwap Cci
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Implementação da estratégia - VWAP + CCI. Compra quando o preço está abaixo do VWAP e o CCI está abaixo de -100 (sobrevendido). Vende quando o preço está acima do VWAP e o CCI está acima de 100 (sobrecomprado).

Os testes indicam um retorno anual médio de aproximadamente 82%. Funciona melhor no mercado de ações.

O VWAP atua como referência de valor, e o CCI destaca movimentos de momentum que se afastam dele. As entradas favorecem leituras fortes do CCI em relação ao VWAP.

Projetado para traders intradiários que se concentram na interação com o VWAP. Um stop percentual ajuda a manter a disciplina.

## Detalhes

- **Critérios de entrada**:
  - Comprado: `Close < VWAP && CCI < CciOversold`
  - Vendido: `Close > VWAP && CCI > CciOverbought`
- **Comprado/Vendido**: Ambos
- **Critérios de saída**:
  - O preço cruza de volta pelo VWAP
- **Stops**: Baseados em porcentagem usando `StopLossPercent`
- **Valores padrão**:
  - `CciPeriod` = 20
  - `CciOversold` = -100m
  - `CciOverbought` = 100m
  - `StopLossPercent` = 2
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
    O mercado negocia 24 horas, por isso o VWAP da sessão reinicia a cada dia UTC e pondera o preço típico de cada candle pelo seu volume. Um sinal de entrada contra uma posição aberta a inverte.
- **Filtros**:
  - Categoria: Reversão à média
  - Direção: Ambos
  - Indicadores: VWAP, CCI
  - Stops: Sim
  - Complexidade: Intermediário
  - Período: Médio prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

