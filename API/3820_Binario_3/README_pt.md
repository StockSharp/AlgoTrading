# Estratégia Binário 3
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral

Esta estratégia é uma versão StockSharp do MetaTrader 4 especialista "Binario_3" de `MQL/7658/Binario_3.mq4`. O EA original envolve o mercado com duas médias móveis exponenciais de 144 períodos calculadas nas máximas e mínimas das velas e negocia o rompimento deste canal adaptativo. As ordens de stop pendentes são colocadas acima da banda superior e abaixo da banda inferior, enquanto as paradas de proteção, as metas de lucro e um trailing stop opcional emulam o comportamento MetaTrader.

A versão StockSharp mantém as mesmas regras de decisão, mas é implementada com o API de alto nível:

1. Assina a série de velas configuradas e recalcula os dois envelopes EMA sempre que uma vela é concluída.
2. Quando o último fechamento permanece dentro do canal, coloca ordens de compra e venda no deslocamento necessário dos valores EMA.
3. Registra os níveis de stop-loss e take-profit associados a cada ordem pendente para que possam ser aplicados à posição assim que a ordem for atendida.
4. Rastreia cotações de nível 1 para gerenciar posições abertas: fecha negociações se o preço atingir o stop-loss registrado, o alvo ou a distância do trailing stop.
5. Cancela ordens pendentes se o preço sair do canal ou se a posição oposta se tornar ativa, espelhando a lógica de limpeza no script MQL.

## Parâmetros

| Nome | Padrão | Descrição |
|------|---------|-------------|
| `TakeProfit` | `850` pontos | Distância adicional (em pontos) adicionada ao lado do rompimento ao calcular o lucro. |
| `TrailingStop` | `850` pontos | Distância em pontos usados para saídas de fuga. Defina como `0` para desativar o rastreamento. |
| `PipDifference` | `25` pontos | Compensação do canal EMA antes de colocar pedidos pendentes. |
| `Lots` | `0.1` | Volume base de negociação utilizado quando o dimensionamento baseado no risco não pode ser obtido. |
| `MaximumRisk` | `10` | Multiplicador de risco copiado do EA original. A estratégia estima o volume como `max(Lots, Balance * MaximumRisk / 50000)`. |
| `EmaPeriod` | `144` | Período das médias móveis exponenciais construídas sobre preços altos e baixos. |
| `CandleType` | `1 hour` período de tempo | Série de velas que impulsiona atualizações de indicadores e colocação de pedidos. |

Todos os pontos são convertidos em distâncias de preços reais usando o `PriceStep` do instrumento. Se o símbolo não expor uma etapa, a estratégia volta para `1`.

## Lógica de negociação

1. **Cálculos de indicadores** – Duas instâncias `ExponentialMovingAverage` processam os preços máximos e mínimos da vela. Os pedidos são gerados somente depois que ambas as médias estiverem totalmente formadas.
2. **Ordens pendentes** – Quando o preço de fechamento está dentro do canal, as ordens stop de compra e stop de venda são colocadas em:
   - Stop de compra: EMA(máximo) + spread + `PipDifference` * passo.
   - Parada de venda: EMA(baixo) - `PipDifference` * passo.
Os valores de stop-loss e take-profit associados a essas ordens são armazenados até que a posição se torne ativa.
3. **Gerenciamento de posição** – Assim que uma posição é aberta, a estratégia cancela a ordem pendente oposta e adota os níveis de stop/alvo armazenados. As cotações de nível 1 são monitoradas para fechar a negociação se o mercado atingir o stop-loss, o take-profit ou a distância do trailing stop (`TrailingStop` * step).
4. **Trailing stop** – Para posições longas, o nível móvel segue o melhor lance quando o lucro excede a distância configurada; para shorts o nível segue o melhor pedido. O nível final apenas se move na direção da negociação, reproduzindo o comportamento final MetaTrader.
5. **Limpeza de pedidos** – Quando o último fechamento sai do canal EMA, ambos os pedidos pendentes são cancelados para evitar entradas indesejadas, correspondendo às verificações de segurança do script original.

## Diferenças da versão MQL

- O EA original modificou as ordens de parada do lado do servidor com `OrderModify`; a porta StockSharp simula o mesmo efeito observando cotações de nível 1 e chamando `ClosePosition()` quando uma parada ou meta é atingida.
- Os trailing stops são implementados inteiramente dentro da estratégia porque os pedidos StockSharp de alto nível não suportam instruções de trailing na bolsa.
- O cálculo do volume utiliza o saldo do portfólio (`Portfolio.CurrentValue` ou `Portfolio.BeginValue`) quando disponível. Se o saldo não for conhecido, a estratégia volta ao valor `Lots` configurado.
- Os preços são normalizados de acordo com a etapa de preço do instrumento antes de registrar as ordens para mantê-los alinhados com as exigências do câmbio.

## Notas de uso

- Habilite assinaturas de nível 1 ao executar a estratégia para que os trailing stops e as saídas de proteção possam reagir às atualizações de compra/venda em tempo real.
- A estratégia depende de velas concluídas. Se o intervalo de tempo selecionado for muito grande, o tempo de resposta refletirá esse ritmo mais lento.
- O rastreamento pode ser desativado definindo `TrailingStop` como `0`. Neste modo, apenas os níveis fixos de stop-loss e take-profit são usados.
