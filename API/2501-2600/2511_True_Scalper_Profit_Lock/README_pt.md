# Estratégia True Scalper com Bloqueio de Lucro
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão Geral
A **Estratégia True Scalper com Bloqueio de Lucro** é um port do StockSharp do consultor especializado do MetaTrader 5 "True Scalper Profit Lock". A estratégia foca em trading de ultra curto prazo usando médias móveis exponenciais rápidas, um filtro RSI de dois períodos e uma rotina de proteção de lucros que move os stops para o break-even. A lógica adicional de "abandono" força a estratégia a fechar trades que não atingem o alvo dentro de um número predefinido de velas.

A implementação assina um único stream de velas e avalia apenas as velas concluídas. É projetada para scalping intradiário, mas todos os parâmetros são totalmente ajustáveis, permitindo adaptá-la a outros períodos ou instrumentos.

## Indicadores e Dados
- **EMA (rápida)** – comprimento padrão 3, atua como gatilho altista quando cruzar acima da EMA lenta.
- **EMA (lenta)** – comprimento padrão 7, define a direção da tendência de curto prazo.
- **RSI** – comprimento padrão 2 com modo de decisão selecionável:
  - *Método A* (desabilitado por padrão) reage ao RSI cruzando o limiar da vela anterior.
  - *Método B* (habilitado por padrão) rastreia a polaridade do RSI em relação ao limiar.
- **Velas** – o período padrão é 1 minuto, configurável através do parâmetro `CandleType`.

## Lógica de Entrada
1. Calcular a EMA rápida, EMA lenta e RSI na última vela concluída.
2. Avaliar o estado do RSI:
   - Método A: definir a polaridade do RSI apenas quando o limiar for cruzado entre duas velas consecutivas.
   - Método B: definir a polaridade do RSI de acordo com se o valor está acima ou abaixo do limiar.
3. **Setup de compra** – ativado quando a EMA rápida está pelo menos um passo de preço acima da EMA lenta *e* o RSI indica polaridade negativa. Se a lógica de abandono forçou uma inversão para comprado, o trade também é aberto independentemente dos sinais atuais.
4. **Setup de venda** – ativado quando a EMA rápida está pelo menos um passo de preço abaixo da EMA lenta *e* o RSI indica polaridade positiva, ou quando uma inversão de abandono impõe uma entrada vendida.
5. As inversões de posição são tratadas enviando a diferença necessária para inverter a posição líquida em uma única ordem de mercado.

## Lógica de Saída
- **Stop Loss / Take Profit** – configurados em passos de preço (`StopLossPoints`, `TakeProfitPoints`) e aplicados imediatamente após a entrada.
- **Bloqueio de lucro** – quando habilitado, uma vez que o trade aberto acumula o lucro especificado (`BreakEvenTriggerPoints`), o stop é movido para break-even mais um offset (`BreakEvenPoints`). A rotina funciona para posições compradas e vendidas e é executada apenas uma vez por trade.
- **Lógica de abandono** – rastreia o número de velas concluídas desde a entrada:
  - *Método A*: fecha o trade após `AbandonBars` velas e define uma flag para abrir uma posição na direção oposta na próxima oportunidade.
  - *Método B*: fecha a posição após o tempo limite, mas deixa intacta a seleção de direção baseada em sinal.
  - O Método A tem prioridade quando ambos os métodos estão habilitados.
- As saídas manuais são emitidas com ordens de mercado (via `ClosePosition`) e redefinem automaticamente o estado do trade.

## Gestão do Dinheiro
- Quando `UseMoneyManagement` está habilitado, o tamanho da posição é derivado do saldo do portfólio: `Ceiling(Balance * RiskPercent / 10000) / 10`.
- O volume gerenciado está limitado às regras originais do MT5: fallback mínimo para `InitialVolume`, valores acima de 1 lote arredondados para cima, multiplicador opcional de mini-conta, limite máximo de 100 lotes.
- Quando desabilitado, a estratégia usa o `InitialVolume` fixo para cada ordem.

## Parâmetros
- `InitialVolume` – tamanho de lote base quando a gestão do dinheiro está desabilitada.
- `TakeProfitPoints` / `StopLossPoints` – distância em unidades de `Security.PriceStep`.
- `FastPeriod`, `SlowPeriod`, `RsiLength`, `RsiThreshold` – configuração de indicadores.
- `UseRsiMethodA`, `UseRsiMethodB` – alternar a lógica de decisão do RSI.
- `UseAbandonMethodA`, `UseAbandonMethodB`, `AbandonBars` – configurar o gerenciamento de tempo limite.
- `UseMoneyManagement`, `RiskPercent`, `LiveTrading`, `IsMiniAccount` – opções de dimensionamento de risco alinhadas com o consultor especializado MT5.
- `UseProfitLock`, `BreakEvenTriggerPoints`, `BreakEvenPoints` – parâmetros de break-even.
- `MaxPositions` – mantido para compatibilidade com a versão MQL (o port do StockSharp gerencia uma única posição líquida por instrumento).
- `CandleType` – período ou tipo de vela personalizado para geração de sinais.

## Notas de Uso
- Anexe a estratégia a um único instrumento; o override `GetWorkingSecurities` assina automaticamente o tipo de vela selecionado.
- As funcionalidades de bloqueio de lucro e abandono dependem de velas concluídas; picos de preço intrabar que revertem dentro da mesma vela são ignorados.
- O parâmetro original MT5 `Slippage` não foi usado no código-fonte e, portanto, não está presente.
- Ajuste `Security.PriceStep` ou os parâmetros baseados em passos de acordo com o instrumento negociado para manter as distâncias em pips pretendidas.

## Diferenças de Conversão
- O StockSharp opera em posições líquidas, portanto múltiplas posições simultâneas não são abertas mesmo se `MaxPositions` for maior que um. Isso reflete o comportamento típico de netting do consultor especializado original quando `maxTradesPerPair` é igual a 1.
- O gerenciamento de ordens usa os helpers `BuyMarket`, `SellMarket` e `ClosePosition` em vez de manipulação direta de tickets.
- Os dados do indicador são entregues através de callbacks `Bind` para evitar acesso manual ao buffer.

## Recomendações de Teste
- Valide o comportamento em dados históricos com o mesmo período usado no EA original (velas de 1 minuto).
- Otimize `TakeProfitPoints`, `StopLossPoints` e `BreakEvenTriggerPoints` para o instrumento alvo, pois estes foram ajustados para cotações forex.
