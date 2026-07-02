# TDS Global 4
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
TDSGlobal 4 é uma conversão do consultor especialista MetaTrader 4 "TDSGlobal 4". O sistema original aplica o triplo de Alexander Elder
método de tela combinando a inclinação de um histograma diário MACD (OsMA) com um filtro Williams %R. Os pedidos só são implantados quando
O momentum diário se alinha com os extremos do oscilador, após o que a estratégia coloca o intervalo do dia anterior entre parênteses com estoques pendentes.
p ordens. A porta StockSharp mantém a mesma lógica de breakout, adiciona agendamento preciso para que diferentes símbolos FX sejam disparados em estágio
minutos vermelhos e gerencia a exposição aberta com trailing stops opcionais, além de metas de lucro configuráveis.

## Lógica estratégica
### Filtros de prazo mais altos
* **MACD inclinação** – compara os dois últimos valores principais diários concluídos MACD (EMA rápida 12, EMA lenta 26, sinal EMA 9). O preconceito é b
Ullish quando o valor mais recente excede o anterior, baixista quando é menor e neutro quando são iguais.
* **Williams %R** – avalia o Williams %R diário (período 24). Configurações longas são permitidas somente quando a leitura está acima do limite superior
limite (padrão -25, significando força de sobrecompra), enquanto configurações curtas exigem que o valor permaneça abaixo do limite inferior (de
falha −75).

### Posicionamento de breakout
* **Níveis de preços** – em cada vela diária concluída, a estratégia registra a máxima e a mínima do dia anterior. Novas ordens de stop são pos
indicou um pip além desses extremos (configurável via *EntryBufferPips*), imitando o deslocamento de ±1 ponto do EA original.
* **Guarda de distância** – antes de enviar uma ordem de stop, o código impõe uma lacuna mínima entre a melhor cotação atual e o preço de entrada
gelo (padrão 16 pips, correspondendo à verificação de 16 *pontos* do EA). Isso evita que ordens pendentes sejam descartadas muito perto do mercado.
t quando a volatilidade é baixa.
* **Gating direcional** – os limites de compra são criados somente quando a inclinação MACD é positiva e o Williams %R confirma a tendência de alta. S
Todas as paradas exigem uma inclinação negativa e um Williams%R que indica pressão de baixa.

### Manutenção de pedido pendente
* **Reinicialização diária** – quando uma nova vela diária fecha, todas as ordens pendentes restantes são canceladas para que a próxima sessão de negociação comece
ts com uma lousa limpa. Se os filtros não permitirem uma negociação, nenhum pedido será feito para esse dia.
* **Uma negociação por dia** – uma vez avaliadas as ordens para um determinado dia (sejam elas colocadas ou ignoradas), a estratégia espera
s para o próximo fechamento diário antes de reavaliar. Ordens de stop preenchidas cancelam automaticamente o lado oposto para evitar baixas simultâneas
longa/curta exposição.

### Gestão de risco
* **Paradas de proteção** – as posições longas herdam uma saída de proteção logo abaixo da mínima do dia anterior, enquanto as posições curtas usam o
alta anterior. Esses níveis são monitorados no fluxo de disparo de um minuto.
* **Take Profit** – metas fixas opcionais expressas em pips em relação ao preço de preenchimento real. Defina *TakeProfitPips* como `0` para dis
capaz de atingir o alvo, espelhando a configuração MT4.
* **Trailing stop** – se *TrailingStopPips* for maior que zero, a estratégia lê as melhores cotações de compra/venda dos dados e trilha do Nível 1
é o stop quando o preço se move a favor da negociação. Quando o nível móvel é violado, a posição é fechada no mercado.

### Agendamento
* **Janelas de minutos** – para evitar envios simultâneos em diferentes pares de moedas, o EA usou janelas de minutos específicas do símbolo
era. A porta replica este comportamento: USDCHF usa minutos 0/8/16/24/32/40/48, GBPUSD 2/10/18/26/34/42/50, USDJPY 4/12/20/28/36/44
/52 e EURUSD 6/14/22/30/38/46/54. Qualquer outro instrumento volta para a hora inteira (0–59).
* **Trigger stream** – uma assinatura de vela de um minuto orienta tanto o agendamento dos pedidos diários quanto o stop/take intradiário.
verificações de lucro. A avaliação real do sinal ocorre apenas uma vez por dia de negociação durante o primeiro minuto elegível.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `Volume` | Volume de pedidos para entradas stop. | `1` |
| `MacdFastPeriod` / `MacdSlowPeriod` / `MacdSignalPeriod` | Configuração MACD usada para medir a inclinação diária. | `12 / 26 / 9` |
| `WilliamsPeriod` | Lookback para o filtro Williams %R. | `24` |
| `WilliamsBuyLevel` | Limite superior (normalmente -25) necessário antes que pedidos longos sejam habilitados. | `-25` |
| `WilliamsSellLevel` | Limite inferior (normalmente -75) necessário antes que as ordens curtas sejam habilitadas. | `-75` |
| `TakeProfitPips` | Distância de lucro em pips; `0` desativa o alvo. | `999` |
| `TrailingStopPips` | Distância do trailing stop em pips; `0` desativa o rastreamento. | `10` |
| `EntryBufferPips` | Compensação adicionada além da máxima/mínima do dia anterior antes de colocar uma ordem de stop. | `1` |
| `MinDistancePips` | Distância mínima do pip da cotação atual até a ordem pendente. | `16` |
| `DailyCandleType` | Período que alimenta os filtros MACD e Williams %R. | `1 day` velas |
| `TriggerCandleType` | Menor prazo utilizado para agendamento e monitoramento de pedidos. | `1 minute` velas |

## Notas adicionais
* A implementação C# depende inteiramente de ajudantes StockSharp de alto nível (`SubscribeCandles`, `BuyStop`, `SellStop`, nível1 bindi
ng) para que possa ser reutilizado dentro da plataforma sem encanamento manual.
* Os dados de nível 1 são necessários para a operação de trailing stop porque o algoritmo usa as melhores cotações de compra/venda para mover e acionar o
parada virtual.
* O pacote não inclui tradução para Python; apenas a estratégia C# e a documentação multilíngue são fornecidas.
