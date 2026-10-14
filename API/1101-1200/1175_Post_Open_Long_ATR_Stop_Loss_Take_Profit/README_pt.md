# Estratégia de Comprado Pós-Abertura com ATR Stop Loss e Take Profit
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia entra em uma posição comprada durante a abertura do mercado após um rompimento de resistência enquanto o preço permanece próximo da banda média de Bollinger. Utiliza filtros EMA, RSI, ADX e ATR e sai por meio de stop loss e take profit baseados em ATR.

## Detalhes

- **Critérios de entrada**:
  - **Comprado**: Rompimento acima da resistência recente durante a abertura do mercado, preço próximo à banda média de Bollinger, RSI acima do limite, ADX acima do limite, tendência de curto prazo de alta e sem retração.
- **Comprado/Vendido**: Somente comprado.
- **Critérios de saída**:
  - Stop loss ou take profit baseado em ATR atingido.
- **Stops**:
  - Stop loss e take profit baseados em ATR.
- **Valores padrão**:
  - `BB Length` = 14
  - `BB Mult` = 1.5
  - `EMA Length` = 10
  - `EMA Long Length` = 200
  - `RSI Length` = 7
  - `RSI Threshold` = 30
  - `ADX Length` = 7
  - `ADX Threshold` = 10
  - `ATR Length` = 14
  - `ATR SL Mult` = 2.0
  - `ATR TP Mult` = 4.0
  - `SessionStart` = 09:30 UTC
  - `SessionDuration` = TimeSpan.FromHours(1)
  - `ResistancePeriod` = 5
  - `BasisProximity` = 1
  - `CandleType` = TimeSpan.FromMinutes(5).TimeFrame()
- **Filtros**:
  - Categoria: Seguidor de tendência
  - Direção: Long
  - Indicadores: Bollinger Bands, EMA, RSI, ADX, ATR
  - Stops: ATR
  - Complexidade: Intermediário
  - Período: Qualquer
  - Sazonalidade: Não
  - Redes neurais: Não
  - Divergência: Não
  - Nível de risco: Médio

## Cálculo e execução

Sinais usam velas concluídas após todos os indicadores se formarem. Resistência é o maior high das `ResistancePeriod` velas anteriores, excluindo a atual; entrada exige `Close > resistance`. A janela usa horário UTC de abertura da vela: início incluído, fim excluído, com passagem pela meia-noite. Configure `SessionStart` para a abertura do instrumento.

A base Bollinger é a SMA dos fechamentos incluindo a vela atual; desvio populacional. Proximidade: `abs(Close - basis) <= (UpperBand - basis) * BasisProximity`. Exige-se `EMA curta > EMA longa` e `Close > EMA curta`. Sem retração significa `Close >= Open` na vela de sinal. RSI e ADX devem superar estritamente seus limites.

Compra `Volume` apenas sem posição ou ordem pendente. Wilder ATR de entrada fixa as distâncias SL e TP. A proteção local nativa define os níveis abaixo/acima do preço real de execução (ponderado por volume para execuções parciais), observa best bid Level1 e preços de velas e fecha toda a posição comprada restante. Saídas continuam ativas fora da janela. Mudanças posteriores de ATR não movem os níveis. Sem posições vendidas, saídas por cruzamento EMA ou espera.
