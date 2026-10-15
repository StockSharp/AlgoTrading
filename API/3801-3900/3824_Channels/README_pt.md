# Estratégia de Canais
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Esta estratégia é uma porta direta do consultor especialista MetaTrader 4 "Canais" incluído na biblioteca pública de Gordago. Ele combina uma média móvel exponencial muito rápida (EMA) com três envelopes baseados em EMA para detectar momentos em que o preço escapa das zonas comprimidas. Uma vez aberta uma única posição, a estratégia depende de ordens de stop e trailing stops opcionais para gerenciar saídas, assim como a implementação original MQL.

## Lógica de negociação

- A estratégia assina velas horárias por padrão e calcula:
  - Um EMA rápido (comprimento 2) usando preços de **fechamento** de velas.
  - Um segundo EMA rápido (comprimento 2) usando preços de vela **aberto**, exigido pelas regras de entrada curta do consultor especialista.
  - Um EMA lenta (comprimento 220) nos fechamentos que serve de base para três desvios de envelope: ±1,0%, ±0,7% e ±0,3%.
- Uma posição **longa** é aberta quando o fechamento EMA rápida satisfaz qualquer uma das seis verificações cruzadas históricas:
  1. Ele cruza para cima através do envelope externo 1% inferior.
  2. Ele cruza para cima através do envelope inferior de 0,7%.
  3. Ele passa duas barras consecutivas abaixo do envelope inferior de 0,3% (condição de sobrevenda).
  4. Ele cruza para cima através do próprio EMA lento.
  5. Ele cruza para cima através do envelope superior de 0,3%.
  6. Ele cruza para cima através do envelope superior de 0,7%.
- Uma posição **curta** é aberta quando o rápido baseado em abertura EMA aciona qualquer uma das regras curtas simétricas:
  1. Ele cruza para baixo através do envelope superior externo de 1%.
  2. Cruza para baixo através do envelope superior de 0,7%.
  3. Ele cruza para baixo o envelope superior de 0,3%.
  4. Ele cruza para baixo através do EMA lenta.
  5. Ele cruza para baixo através do envelope inferior de 0,3%.
  6. Ele cruza para baixo através do envelope inferior de 0,7%.
- Apenas uma posição de mercado pode existir por vez. Um novo sinal é ignorado enquanto uma negociação está ativa, correspondendo ao comportamento do especialista MetaTrader.

## Gestão de risco

- As distâncias individuais de stop-loss e take-profit podem ser configuradas para negociações longas e curtas. Quando definidas como zero, essas ordens de proteção são ignoradas, o que replica o estado desabilitado por padrão da fonte original.
- Os trailing stops opcionais apertam a ordem de proteção quando o preço se move a favor da posição em mais do que a distância final medida em pontos.
- Todas as ordens de proteção são canceladas automaticamente quando a posição é achatada ou a estratégia é interrompida.

## Parâmetros

| Nome | Descrição |
| ---- | ----------- |
| `Candle Type` | Prazo utilizado para análise de preços (padrão: 1 hora). |
| `Volume` | Tamanho do pedido usado para todas as entradas. |
| `Fast EMA` / `Slow EMA` | Períodos para EMAs rápidos e lentos. |
| `Envelope 1%`, `Envelope 0.7%`, `Envelope 0.3%` | Largura percentual das três faixas do envelope. |
| `Buy Stop-Loss`, `Sell Stop-Loss` | Distância em pontos entre o preço de entrada e o stop loss inicial para negociações longas ou curtas. |
| `Buy Take-Profit`, `Sell Take-Profit` | Distância em pontos para os níveis opcionais de take-profit fixo. |
| `Buy Trailing`, `Sell Trailing` | Distância do trailing stop em pontos para posições longas ou curtas. |
| `Use Trading Hours` | Ativa o filtro de janela de tempo. |
| `From Hour`, `To Hour` | Limites de horas do dia inclusivos para abertura de novas posições. A janela termina por volta da meia-noite se `From` for maior que `To`. |

## Notas de uso

1. Como as distâncias de parada são definidas em pontos, elas são multiplicadas pela segurança `PriceStep` internamente. Certifique-se de que esta etapa corresponda ao instrumento usado para negociação.
2. O comprimento EMA rápida é intencionalmente muito curto para espelhar o especialista MT4. Aumentá-lo mudará drasticamente a frequência do sinal.
3. O consultor original também permitiu listas de permissões de contas e alertas sonoros. Eles foram omitidos porque são específicos da plataforma e não afetam a lógica do pedido.
