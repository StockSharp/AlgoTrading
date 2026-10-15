# Glam Trader (confirmação de vários prazos)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A estratégia replica o consultor especialista original MetaTrader "GLAM Trader" combinando informações de três períodos de tempo:

- Um **EMA(3)** rápido no gráfico de 15 minutos captura o viés da tendência de curto prazo.
- Um **filtro Laguerre** com gama 0,7 aplicado a velas de 5 minutos mede se o preço está sendo negociado acima ou abaixo de seu caminho suavizado.
- O **Awesome Oscillator** em velas horárias fornece uma verificação de impulso alinhada com a definição de Bill Williams.

Somente quando todos os três componentes concordam é que a estratégia abre uma negociação, com o objetivo de filtrar o ruído que apareceria quando qualquer período de tempo fosse avaliado isoladamente.

## Lógica de negociação
1. **Preparação de dados**
   - Velas de 15 minutos alimentam um `ExponentialMovingAverage` com comprimento `EmaPeriod` (padrão 3).
   - Velas de 5 minutos alimentam um `LaguerreFilter` com suavização `LaguerreGamma`.
   - Velas de 60 minutos alimentam um `AwesomeOscillator`.
   - Para cada período de tempo, o último fechamento da vela finalizado é armazenado para reproduzir a comparação original do indicador versus preço.
2. **Condições de entrada**
   - **Longo**: o EMA está acima do fechamento atual de 15 minutos, Laguerre está acima do último fechamento de 5 minutos e Awesome Oscillator está acima do último fechamento horário.
   - **Short**: cada um dos três indicadores deve ficar abaixo do fechamento correspondente.
3. **Gerenciamento de riscos**
   - Distâncias separadas de stop-loss e take-profit (expressas em pontos de instrumento) para negociações longas e curtas.
   - Os trailing stops são ativados quando o preço percorre pelo menos a distância móvel especificada além do preço de entrada. A parada é aumentada na direção da tendência sem recuar.
   - Todas as ações de proteção (take-profit, stop-loss, trailing stop) fecham toda a posição com ordens de mercado, espelhando a implementação MQL.

## Parâmetros
| Nome | Descrição | Padrão |
| --- | --- | --- |
| `TradeVolume` | Tamanho do pedido para novas posições. | 0,1 |
| `PrimaryCandleType` | Período usado para EMA e sinal principal. | Velas de 15 minutos |
| `LaguerreCandleType` | Prazo analisado pelo filtro Laguerre. | Velas de 5 minutos |
| `AwesomeCandleType` | Prazo analisado pelo Awesome Oscillator. | Velas de 60 minutos |
| `EmaPeriod` | Duração de EMA no período principal. | 3 |
| `LaguerreGamma` | Parâmetro gama para o filtro Laguerre. | 0,7 |
| `LongStopLossPoints` | Distância stop-loss para negociações longas, em pontos. | 20 |
| `ShortStopLossPoints` | Distância stop-loss para negociações curtas, em pontos. | 20 |
| `LongTakeProfitPoints` | Distância de lucro para negociações longas, em pontos. | 50 |
| `ShortTakeProfitPoints` | Distância de lucro para negociações curtas, em pontos. | 50 |
| `LongTrailingPoints` | Distância final para negociações longas, em pontos. | 15 |
| `ShortTrailingPoints` | Distância final para negociações curtas, em pontos. | 15 |

## Notas
- A estratégia assina três fluxos de velas independentes e mantém apenas os valores finalizados mais recentes, evitando buffers manuais de histórico.
- Todos os comentários e mensagens de registro permanecem em inglês para maior clareza, correspondendo às convenções do projeto.
- Ajuste os parâmetros de risco baseados em pontos de acordo com o `PriceStep` do instrumento para que os níveis de proteção reflitam o tamanho do tick da corretora.
