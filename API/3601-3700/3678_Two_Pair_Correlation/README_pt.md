# Estratégia de correlação de dois pares
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

A **Estratégia de correlação de dois pares** transporta o consultor especialista MetaTrader *"Correlação de 2 pares EA"* (pacote `MQL/52043`) para o StockSharp API de alto nível. Ele observa os preços de oferta de dois símbolos criptográficos altamente correlacionados (BTCUSD como perna primária e ETHUSD como perna de hedge) e realiza uma negociação neutra em termos de mercado quando seu spread se desvia de um limite configurável.

### Fluxo de trabalho principal

1. **Restrição de risco** – o patrimônio do portfólio é monitorado continuamente. Se o rebaixamento do pico histórico exceder `MaxDrawdownPercent`, novas negociações serão suspensas até que o patrimônio se recupere acima de `RecoveryPercent` do valor de pico.
2. **Filtro de volatilidade** – ambos os instrumentos alimentam um fluxo de vela de 5 minutos em um indicador `AverageTrueRange` de comprimento `AtrPeriod`. A negociação é ignorada quando ATR excede `PriceDifferenceThreshold * 0.01`, imitando a "pausa de alta volatilidade" do código MQL.
3. **Detecção de spread** – a estratégia assina dados de nível um para ambos os instrumentos e avalia o spread do preço de compra em cada atualização. Quando `Bid(BTCUSD) - Bid(ETHUSD) > PriceDifferenceThreshold`, compra BTCUSD e vende ETHUSD. Quando o spread cai abaixo de `-PriceDifferenceThreshold`, as posições são invertidas (short BTCUSD, long ETHUSD).
4. **Dimensionamento dinâmico de lote** – o volume por perna é derivado de `RiskPercent` do patrimônio atual do portfólio, dividido pela distância de stop sintética `StopLossPips * PriceStep`. O resultado é normalizado com as restrições de volume de câmbio antes do envio dos pedidos.
5. **Saída da cesta** – o lucro flutuante total de ambas as pernas é rastreado na moeda da conta. Ao atingir `MinimumTotalProfit`, a estratégia fecha o par inteiro, independentemente da direção de entrada.

## Dados de mercado necessários

- **Nível1** (melhor oferta/venda) tanto para o título primário (`Security`) quanto para o título de hedge (`SecondSecurity`).
- **Velas** do tipo `AtrCandleType` (o padrão é o período de 5 minutos) para os mesmos dois instrumentos para alimentar o filtro ATR.

Certifique-se de que os títulos exponham valores significativos de `PriceStep`, `StepPrice`, `VolumeStep` e volume mínimo/máximo para que o tamanho do lote e a conversão de lucro espelhem o comportamento de MetaTrader.

## Parâmetros

| Nome | Tipo | Padrão | Descrição |
| ---- | ---- | ------- | ----------- |
| `SecondSecurity` | `Security` | - | Instrumento de hedge (ETHUSD no original EA). |
| `MaxDrawdownPercent` | `decimal` | `20` | Limite de rebaixamento que pausa novas negociações. |
| `RiskPercent` | `decimal` | `2` | Participação da carteira arriscada por negociação para dimensionamento de posição. |
| `PriceDifferenceThreshold` | `decimal` | `100` | Divergência do preço de compra necessária para abrir o par. |
| `MinimumTotalProfit` | `decimal` | `0.30` | Meta de lucro na moeda da conta para fechamento de ambas as pernas. |
| `AtrPeriod` | `int` | `14` | Comprimento ATR para o filtro de volatilidade. |
| `RecoveryPercent` | `decimal` | `95` | Porcentagem do pico de patrimônio necessário para retomar a negociação após uma redução. |
| `StopLossPips` | `int` | `50` | Parada sintética usada para converter `RiskPercent` em lotes. |
| `AtrCandleType` | `DataType` | `TimeSpan.FromMinutes(5).TimeFrame()` | Série de velas usada para cálculo de ATR. |

## Arquivos

- `CS/TwoPairCorrelationStrategy.cs` – implementação de estratégia baseada no API de alto nível.
- `README.md` – esta documentação (inglês).
- `README_zh.md` – documentação em chinês.
- `README_ru.md` – documentação em russo.
