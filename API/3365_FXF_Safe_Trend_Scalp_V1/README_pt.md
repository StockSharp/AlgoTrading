# Estratégia FXF Safe Trend Scalp V1 (C#)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A estratégia FXF Safe Trend Scalp V1 negocia rompimentos de linhas de tendência baseadas em ZigZag e reflete o comportamento do consultor especialista MetaTrader 4 original. Ele observa a distância entre o preço atual e as linhas dinâmicas de resistência/suporte construídas a partir de pivôs ZigZag recentes e alinha as negociações com um par de médias móveis simples. Stop-loss protetor, take-profit e saída de lucro flutuante reproduzem as regras de gestão de dinheiro do código-fonte.

## Lógica de negociação

1. **Linhas de tendência em ziguezague**
   - Um detector ZigZag manual procura altos e baixos alternados de oscilação usando os parâmetros configuráveis de profundidade, desvio e retrocesso.
   - Os últimos quatro máximos de oscilação definem a linha de resistência ativa, enquanto os últimos quatro mínimos de oscilação definem a linha de suporte ativa. A estratégia extrapola continuamente essas linhas para a barra atual.
   - Um sinal de entrada é preparado quando o preço de fechamento se aproxima de uma linha dentro de um deslocamento fixo (10 pontos por padrão).
2. **Filtro de média móvel**
   - Uma média móvel simples rápida (comprimento 2) e uma média móvel simples lenta (comprimento 50) filtram a tendência.
   - As posições curtas exigem a MM rápida abaixo da MM lenta, enquanto as posições longas exigem a MM rápida acima da MM lenta.
3. **Execução de pedido**
   - Os sinais são armazenados e ativados na próxima vela concluída, correspondendo à lógica de “nova barra” da versão MetaTrader.
   - Antes de abrir uma posição, a estratégia verifica se o spread não ultrapassa o máximo configurado e se nenhuma posição está aberta no momento.
4. **Gerenciamento de Riscos**
   - As distâncias de stop-loss e take-profit são expressas em pontos e aplicadas imediatamente após o pedido ser atendido.
   - Uma meta de lucro flutuante fecha a posição quando o lucro não realizado (em unidades de preço vezes o volume) excede a recompensa configurada por lote.

## Parâmetros

| Nome | Descrição |
| --- | --- |
| `Candle Type` | Período de tempo usado para geração de sinal. |
| `Volume` | Volume de negociação enviado com cada entrada. |
| `ZigZag Depth` | Número mínimo de barras entre pivôs confirmados. |
| `ZigZag Deviation (pts)` | O preço mínimo se move em pontos antes da mudança de direção. |
| `ZigZag Backstep` | Barras necessárias antes de aceitar um pivô oposto. |
| `Trend Offset (pts)` | Distância da linha de tendência que aciona um sinal. |
| `Fast MA Length` | Comprimento da média móvel simples rápida. |
| `Slow MA Length` | Comprimento da média móvel simples lenta. |
| `Max Spread (pts)` | Spread máximo permitido, expresso em pontos. |
| `Stop Loss (pts)` | Distância de parada protetora medida a partir do preço de entrada. |
| `Take Profit (pts)` | Distância alvo de lucro medida a partir do preço de entrada. |
| `Profit Target per Lot` | Lucro flutuante necessário (unidades de preço x volume) para fechar a posição. |

## Notas

- Apenas uma posição é mantida por vez. Os sinais são ignorados enquanto uma negociação está aberta.
- O filtro de spread depende das melhores cotações de compra/venda, portanto a estratégia deve estar conectada a uma fonte de dados que forneça informações de nível 1.
- A versão Python da estratégia é omitida intencionalmente conforme solicitado.

## Arquivos

- `CS/FXFSafeTrendScalpV1Strategy.cs` – StockSharp implementação do consultor especialista.
