# Estratégia RSI Expert com Filtro de Tendência
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- Conversão do expert advisor do MetaTrader 5 **RSI_Expert_v2.0** para a API de estratégia de alto nível do StockSharp.
- Gera sinais no `CandleType` configurado (padrão 1 hora) e executa operações no fechamento da vela.
- Projetado para posições líquidas: a estratégia mantém uma única posição agregada em vez de fazer hedge de múltiplos tickets.

## Lógica de entrada
1. **Cruzamento de RSI** – uma configuração comprada aparece quando o último valor do RSI sobe acima de `RsiLevelDown` enquanto a vela finalizada anterior estava abaixo do nível. Uma configuração vendida é acionada quando o RSI cai abaixo de `RsiLevelUp` depois de estar acima.
2. **Filtro de média móvel** – o expert original permite operar com ou contra um cruzamento de média móvel. O parâmetro `MaMode` reproduz as opções:
   - `Off`: ignorar médias móveis e operar apenas com gatilhos RSI.
   - `Forward`: permitir compradas apenas quando a MA rápida está acima da MA lenta, vendidas apenas quando está abaixo.
   - `Reverse`: inverter o filtro para que as compradas exijam a MA rápida abaixo da MA lenta, correspondendo ao modo "reverso" do EA.

Ambas as condições devem concordar antes de a estratégia abrir uma nova ordem de mercado. Se uma posição já está aberta ou uma ordem está aguardando, novos sinais são ignorados até que termine.

## Gerenciamento de operações
- O stop-loss inicial e o take-profit são expressos em pips usando o `PriceStep` do instrumento. Ambos são opcionais; definir um valor de zero desabilita a saída respectiva.
- Quando `TrailingStopPips` é maior que zero, o stop seguirá o preço uma vez que o lucro exceda `TrailingStopPips + TrailingStepPips`. O valor do step deve ser estritamente positivo quando o trailing está habilitado (a estratégia lança uma exceção caso contrário).
- Se `UseMartingale` está habilitado, o próximo volume de ordem dobra após a posição anterior ter fechado com perda (detectado via PnL realizado). Operações vencedoras reiniciam o multiplicador.

## Gestão de capital
- `MoneyMode = FixedVolume` mantém o mesmo `VolumeOrRiskValue` para cada entrada.
- `MoneyMode = RiskPercent` trata `VolumeOrRiskValue` como uma porcentagem do patrimônio do portfólio e deriva a quantidade a partir da distância do stop-loss configurado. Quando nenhum stop-loss é especificado, a estratégia recorre ao valor bruto.
- Os volumes são normalizados para as regras da bolsa usando `Security.MinVolume` e `Security.VolumeStep` para evitar tamanhos de ordem inválidos.

## Notas adicionais de implementação
- A lógica de trailing e as verificações de stop/alvo são avaliadas em velas finalizadas para replicar o comportamento de "nova barra" da versão MQL.
- A flag de martingale usa mudanças de PnL realizado quando uma posição é fechada externamente, para que fechamentos manuais também sejam rastreados.
- Como o StockSharp usa posições agregadas, operações longas e curtas simultâneas (modo de hedge MT5) não são suportadas.

## Parâmetros
| Nome | Descrição |
| --- | --- |
| `CandleType` | Período usado para atualizações de indicadores e geração de sinais. |
| `StopLossPips` | Distância inicial do stop-loss em pips; zero desabilita o stop. |
| `TakeProfitPips` | Distância inicial do take-profit em pips; zero desabilita o alvo. |
| `TrailingStopPips` | Distância do trailing stop. Requer um `TrailingStepPips` positivo. |
| `TrailingStepPips` | Pips adicionais necessários antes que o trailing stop se mova novamente. |
| `MoneyMode` | Seleciona dimensionamento de lote fixo ou cálculo por porcentagem de risco. |
| `VolumeOrRiskValue` | Tamanho do lote no modo fixo ou porcentagem de risco no modo de risco. |
| `UseMartingale` | Dobra o próximo volume de ordem após uma operação perdedora. |
| `FastMaPeriod` | Período da média móvel rápida usada pelo filtro de tendência. |
| `SlowMaPeriod` | Período da média móvel lenta usada pelo filtro de tendência. |
| `RsiPeriod` | Comprimento de média para o indicador RSI. |
| `RsiLevelUp` | Limiar superior do RSI que aciona configurações vendidas. |
| `RsiLevelDown` | Limiar inferior do RSI que aciona configurações compradas. |
| `MaMode` | Habilita ou inverte o filtro de confirmação de média móvel. |
