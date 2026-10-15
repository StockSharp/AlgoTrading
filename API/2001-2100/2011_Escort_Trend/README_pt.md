# Estratégia de Tendência Escort
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A Estratégia de Tendência Escort combina uma Média Móvel Ponderada (WMA) rápida e lenta com confirmação de MACD e CCI. Uma posição comprada é aberta quando a WMA rápida está acima da WMA lenta, a linha principal do MACD cruza acima da linha de sinal e o CCI supera um limiar positivo. Uma posição vendida é acionada nas condições opostas. A estratégia opcionalmente usa stop loss fixo, take profit e trailing stop.

## Detalhes
- **Critérios de entrada**:
  - **Comprado**: `FastWMA > SlowWMA` E `MACD > Signal` E `CCI > +Threshold`.
  - **Vendido**: `FastWMA < SlowWMA` E `MACD < Signal` E `CCI < -Threshold`.
- **Comprado/Vendido**: Ambos os lados.
- **Critérios de saída**:
  - Sinal de entrada oposto.
  - Stop loss, take profit ou trailing stop opcionais.
- **Stops**: Sim, definidos pelo usuário.
- **Valores padrão**:
  - `Fast WMA` = 8
  - `Slow WMA` = 18
  - `CCI Period` = 14
  - `CCI Threshold` = 100
  - `MACD Fast EMA` = 8
  - `MACD Slow EMA` = 18
  - `Take Profit` = 200
  - `Stop Loss` = 55
  - `Trailing Stop` = 35
  - `Trailing Step` = 3
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Ambos
  - Indicadores: Múltiplos
  - Stops: Sim
  - Complexidade: Moderado
  - Período: Curto prazo
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio
