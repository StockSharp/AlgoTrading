# Comerciante Wss
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

Porto do consultor especialista "Wss_trader" MetaTrader 4 publicado em forex-instruments.info. O EA original combina níveis de reversão no estilo Camarilla com distâncias de pivô clássicas e abre uma única negociação por barra sempre que o preço ultrapassa as bandas configuradas durante a sessão de Londres.

## Lógica estratégica

1. No início de cada novo dia de negociação, a estratégia lê a máxima, a mínima e o fechamento diários anteriores para construir uma escada dinâmica:
   - `Pivot = (High + Low + Close) / 3`
   - `Long entry = Pivot + Metric × point`
   - `Short entry = Pivot − Metric × point`
   - `Long stop = Short entry`
   - `Short stop = Long entry`
   - Os alvos espelham as fórmulas MetaTrader `Close ± (High − Low) × 1.1 / 2` com o mesmo grampo de segurança do código original.
2. A negociação só é permitida entre `Start Hour` e `End Hour` (inclusive). Fora da janela, todas as posições abertas são fechadas imediatamente.
3. Quando uma vela finalizada ultrapassa o nível de entrada longo (fechamento >= nível e fechamento anterior <nível), a estratégia compra uma vez com o volume configurado, anexa o stop e o alvo pré-calculados e bloqueia quaisquer entradas adicionais para essa barra. Uma regra simétrica se aplica aos shorts.
4. Se a posição se mover a favor em pelo menos `Trailing Points` passos de preço, o stop será seguido para manter a mesma distância do preço de fechamento. A parada nunca se move para trás.

## Parâmetros

| Nome | Descrição | Padrão |
| ---- | ----------- | ------- |
| `Working Candle` | Tipo de vela principal usado para cálculos intradiários. | `15 Minute` |
| `Daily Candle` | Tipo de vela usado para ler os níveis de pivô do dia anterior. | `1 Day` |
| `Start Hour` | Hora (0-23) em que a negociação está habilitada. | `8` |
| `End Hour` | Hora (0-23) em que a negociação para de aceitar novas entradas. | `16` |
| `Metric Points` | Distância do pivô aos níveis de rompimento medidos em etapas de preços. | `20` |
| `Trailing Points` | Distância do trailing stop em etapas de preço. Defina como `0` para desativar o rastreamento. | `20` |
| `Order Volume` | Tamanho do pedido que reflete o parâmetro `lots` original. | `0.1` |

## Notas

- A estratégia fecha a posição atual assim que a janela de negociação termina, correspondendo ao comportamento do EA original.
- O trailing é processado em velas acabadas. O rastreamento intrabarra não é reproduzido porque StockSharp opera em fechamentos de velas nesta porta.
- É permitida apenas uma negociação por vela, replicando a flag `tenb` da versão MQL.
