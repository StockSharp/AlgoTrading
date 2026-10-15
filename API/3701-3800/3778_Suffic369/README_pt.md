# Estratégia Suff369
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia Suffic369 é um sistema de fuga de acompanhamento de tendências que combina duas médias móveis curtas com bandas Bollinger largas. O consultor especialista entra em posições longas quando a média móvel simples rápida (SMA) dos preços de fechamento cruza acima do SMA das máximas recentes, enquanto o mercado negocia perto da banda inferior Bollinger. As posições curtas são abertas quando o rápido SMA cruza abaixo do SMA dos mínimos recentes enquanto o preço pressiona a banda superior. A versão StockSharp convertida mantém a lógica original MQL, mas a expressa com assinaturas de vela de alto nível e ligações de indicadores.

## Indicadores
- **Rápido SMA (Fecho, duração = 3)** – mede a direção de curto prazo do preço de fechamento.
- **High SMA (High, length = 5)** – calcula a média dos máximos recentes e atua como uma referência de resistência de alta.
- **Low SMA (Low, length = 5)** – calcula a média dos mínimos recentes e fornece a referência de suporte de baixa.
- **Bollinger Bandas (comprimento = 156, desvio = 1)** – identifica extremos de preços em relação à volatilidade.

Todos os indicadores são atualizados nas velas concluídas. Os valores anteriores são armazenados em cache para reproduzir o deslocamento de uma barra usado no programa MetaTrader original.

## Regras de negociação
### Entrada longa
1. O rápido anterior SMA (fechamento) está abaixo da máxima anterior SMA.
2. A corrente rápida SMA (fechada) cruza acima da máxima atual SMA.
3. O preço de fechamento da vela está abaixo da banda inferior Bollinger.

### Entrada curta
1. O rápido anterior SMA (fechamento) está acima do mínimo anterior SMA.
2. A corrente rápida SMA (fechada) cruza abaixo da mínima atual SMA.
3. O preço de fechamento da vela está acima da banda superior Bollinger.

### Sair da lógica
- **Sinal oposto:** Uma posição longa é fechada quando um novo sinal de entrada curto aparece e vice-versa.
- **Stop-Loss:** Stop opcional baseado em etapas de preço que protege a posição uma vez ativada.
- **Take-Profit:** Meta opcional baseada em etapas de preço que reflete o parâmetro TakeProfit original.
- **Trailing Stop:** Trailing Stop opcional que se estreita atrás de negociações lucrativas exatamente como a lógica MQL (usa o fechamento atual para mover o stop somente quando o lucro excede a distância configurada).

A estratégia mantém no máximo uma posição por vez. Depois que um sinal de stop, alvo ou oposto fecha a negociação, nenhuma nova entrada é avaliada até a próxima vela concluída.

## Parâmetros
| Nome | Padrão | Descrição |
|------|---------|-------------|
| `FastMaLength` | 3 | Comprimento do SMA rápido baseado nos preços de fechamento. |
| `HighMaLength` | 5 | Comprimento do SMA calculado nas máximas das velas. |
| `LowMaLength` | 5 | Comprimento do SMA calculado nos mínimos das velas. |
| `BollingerLength` | 156 | Tamanho da janela das bandas Bollinger. |
| `BollingerDeviation` | 1 | Multiplicador de desvio padrão para as bandas. |
| `UseStopLoss` | verdade | Habilita o bloco stop-loss. |
| `StopLossPoints` | 30 | Distância de parada nas etapas de preço do instrumento. |
| `UseTakeProfit` | verdade | Ativa o bloco take-profit. |
| `TakeProfitPoints` | 60 | Distância alvo de lucro em etapas de preço. |
| `UseTrailingStop` | verdade | Permite o gerenciamento de trailing stop. |
| `TrailingStopPoints` | 30 | Deslocamento final nas etapas de preço. |
| `CandleType` | Período de 15 minutos | Tipo de vela usado para cálculos. |

Todos os parâmetros numéricos são expostos como instâncias `StrategyParam<T>` para que possam ser otimizados diretamente dentro de StockSharp.

## Gestão de risco
- Stop-loss, take-profit e trailing stops usam a etapa de preço do instrumento (`Security.PriceStep`) para converter distâncias de pontos em preços absolutos.
- Os trailing stops seguem movimentos lucrativos apenas quando o preço avançou mais do que a distância configurada, replicando a lógica original de modificação de ordem.
- `StartProtection()` é invocado na inicialização para ativar os recursos de proteção integrados do StockSharp.

## Notas de uso
- Assine a estratégia em um instrumento que suporte o tipo de vela selecionado.
- Certifique-se de que a propriedade `Volume` esteja definida para o tamanho de negociação desejado antes de iniciar a estratégia.
- A estratégia aguarda os valores do indicador totalmente formados antes de emitir qualquer ordem; velas iniciais são usadas para semear o histórico do indicador.
