# Estratégia de Pullback do Ouro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Pullback do Ouro combina a direção de tendência com EMA e filtros MACD e TDI. Operações compradas são acionadas quando o preço toca a EMA de 21 períodos durante uma tendência de alta e tanto o MACD quanto o TDI são altistas. Operações vendidas ocorrem em pullbacks para a EMA21 em tendências de baixa com MACD e TDI baixistas. Cada posição usa um take profit e stop loss 1:1 baseado na vela de sinal mais um deslocamento.

## Detalhes
- **Dados**: Velas de preço.
- **Critérios de entrada**:
  - **Comprado**: EMA14 acima da EMA60, vela toca a EMA21, linha MACD acima da linha de sinal, TDI MA acima do sinal TDI e RSI acima de 50.
  - **Vendido**: EMA14 abaixo da EMA60, vela toca a EMA21, linha MACD abaixo da linha de sinal, TDI MA abaixo do sinal TDI e RSI abaixo de 50.
- **Critérios de saída**: Stop loss ou take profit atingido a igual distância da entrada com um deslocamento adicionado.
- **Stops**: `Offset` = 0.1 aplicado à mínima/máxima da vela.
- **Valores padrão**:
  - `EmaFastLength` = 14
  - `EmaSlowLength` = 60
  - `EmaPullbackLength` = 21
  - `SlOffset` = 0.1
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Comprado e Vendido
  - Indicadores: EMA, MACD, RSI, SMA
  - Complexidade: Médio
  - Nível de risco: Médio
