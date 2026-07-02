# Estratégia de amostra TradePad
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **TradePad Sample Strategy** é uma versão do exemplo MetaTrader "TradePad". O consultor especialista original apresentou uma grade de
botões que exibiam a tendência de curto prazo para vários símbolos, colorindo cada célula com o oscilador Stochastic atual
lendo. Esta implementação StockSharp mantém o núcleo analítico da amostra e se concentra no monitoramento de uma lista de instrumentos
sem replicar a interface do usuário na carta. A estratégia assina dados de velas para cada símbolo configurado, calcula um
Stochastic oscilador e classifica cada instrumento em estados *Uptrend*, *Downtrend* ou *Flat*. Cada vez que a classe muda,
a estratégia grava uma mensagem de log semelhante à mudança de cor realizada pelo TradePad original.

A estratégia não faz pedidos. Seu objetivo é ajudar os traders discricionários a acompanhar vários mercados ao mesmo tempo e detectar
mudanças de impulso que exigem ações manuais (por exemplo, troca de gráficos ou preparação de negociações).

## Como funciona

1. **Descoberta de símbolos** – o parâmetro `SymbolList` aceita uma lista de tickers separados por vírgula. Se nenhuma lista for fornecida, o
a estratégia recorre ao `Security` principal atribuído no executor.
2. **Assinatura de vela** – cada símbolo usa o mesmo período de tempo configurado por meio de `CandleType`.
3. **Processamento de indicador** – uma instância `StochasticOscillator` dedicada está vinculada ao fluxo de vela. Quando a vela estiver
concluído, o indicador produz o valor `%K` usado para classificação de tendência.
4. **Classificação de tendência** – uma leitura acima de `UpperLevel` mapeia para *Tendência de alta*, uma leitura abaixo de `LowerLevel` mapeia para *Tendência de baixa*,
tudo no meio é *plano*. O último valor do oscilador é armazenado em `LatestKValues`.
5. **Intervalo de atualização** – a estratégia imita o comportamento do temporizador do TradePad original. Uma alteração é registrada no máximo uma vez por
`TimerPeriodSeconds` para cada símbolo, mesmo que várias velas cheguem dentro do intervalo.

## Parâmetros

| Parâmetro | Descrição |
|-----------|-------------|
| `SymbolList` | Lista separada por vírgulas de instrumentos a serem monitorados. String vazia significa "usar a segurança principal". |
| `TimerPeriodSeconds` | Número mínimo de segundos entre atualizações de estado por símbolo. Evita spam de log quando as velas são muito curtas. |
| `StochasticLength` | Período de lookback usado para calcular a linha `%K` bruta. |
| `StochasticKPeriod` | Período de suavização aplicado à linha `%K`. |
| `StochasticDPeriod` | Período de suavização aplicado à linha `%D` (mantido para fins de integridade, embora a estratégia leia apenas `%K`). |
| `UpperLevel` | Limite acima do qual o símbolo é considerado em tendência de alta. |
| `LowerLevel` | Limite abaixo do qual o símbolo é considerado em tendência de baixa. |
| `CandleType` | Prazo das velas utilizadas para cálculo do indicador. |

## Notas de uso

- Certifique-se de que os tickers especificados estejam disponíveis no conector; símbolos ausentes são relatados no log e ignorados.
- A propriedade `TrendStates` expõe a classificação mais recente para painéis externos ou blocos do Designer.
- Use a estratégia dentro do Designer ou Runner para anexar seus próprios recursos visuais (painéis, gráficos) que reagem ao `AddInfoLog`
mensagens ou os dicionários públicos.
- Como nenhum pedido é enviado, a estratégia pode ser executada com segurança em provedores de dados ativos apenas para fins de monitoramento.

## Comportamento MQL original vs. versão StockSharp

| MQL5 Recurso | StockSharp Implementação |
|--------------|--------------------------|
| Grade gráfica de botões | Exposto como entradas de log e dicionários públicos para que a UI personalizada possa ser criada no Designer. |
| Botões manuais de COMPRA/VENDA | Não implementado; a estratégia permanece intencionalmente passiva. |
| Lógica de arrastar gráfico | Não aplicável em StockSharp e omitido. |
| Atualizações de cores de tendência | Substituído por mudanças de estado de tendência acionadas a cada `TimerPeriodSeconds` por símbolo. |

## Estendendo a Estratégia

- Conecte o dicionário `TrendStates` aos widgets do Designer para reconstruir o bloco colorido usando controles XAML.
- Adicione alertas ou notificações quando um símbolo transitar de *Flat* para *Uptrend* ou *Downtrend*.
- Combine a classificação com a lógica do pedido se quiser automatizar as entradas após identificar um forte impulso.
