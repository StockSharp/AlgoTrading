# Estratégia de níveis MartingaleEA-5 (StockSharp)
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

A **Estratégia de níveis MartingaleEA-5** é uma porta direta do MetaTrader consultor especialista 5 "Níveis MartingaleEA-5" para o StockSharp API de alto nível. O sistema supervisiona uma posição existente e constrói uma grade média de cinco etapas sempre que o mercado se move contra ela. Toda a lógica é executada em velas finalizadas, o que mantém o comportamento reproduzível tanto em testes históricos quanto em negociações ao vivo.

## Lógica de negociação

1. **Monitoramento da exposição existente** – a estratégia espera que uma posição inicial longa ou curta esteja presente. Você pode abrir a primeira negociação manualmente ou através de qualquer outra estratégia.
2. **Detecção de movimento adverso** – em cada vela concluída, a estratégia mede o quão longe o preço atual se afastou da entrada de pior preço do grupo ativo (compra mais alta ou venda mais baixa).
3. **Martingale acréscimos** – se a perda flutuante do grupo for negativa e o movimento adverso ultrapassar as distâncias cumulativas configuradas, a estratégia envia ordens de mercado extras. Cada pedido adicional multiplica o anterior por `VolumeMultiplier`. Podem ser configurados até cinco níveis; o parâmetro `MaxAdditions` limita quantos deles são realmente usados.
4. **Visualização de lucros e perdas** – enquanto um grupo está aberto, a estratégia soma continuamente o PnL não realizado para essa direção. Quando o total atingir `TakeProfitCurrency` ou cair abaixo de `StopLossCurrency`, todas as ordens desse lado serão fechadas com uma ordem de mercado e os contadores de martingale serão zerados.
5. **Normalização de volume** – todo volume de pedido passa pelos `VolumeStep`, `MinVolume` e `MaxVolume` do instrumento para evitar o envio de quantidades não executáveis.

## Parâmetros

| Nome | Descrição | Padrão |
| --- | --- | --- |
| `EnableMartingale` | Ativa ou desativa a lógica de média e liquidação. | `true` |
| `VolumeMultiplier` | Fator aplicado ao volume do pedido anterior ao adicionar um novo nível. | `2.0` |
| `MaxAdditions` | Número máximo de passos de martingale por direção (até cinco). | `4` |
| `Level1DistancePips` | Distância adversa inicial (em pips) antes de abrir a segunda ordem. | `300` |
| `Level2DistancePips` | Distância adicional necessária para o terceiro pedido. | `400` |
| `Level3DistancePips` | Distância adicional necessária para o quarto pedido. | `500` |
| `Level4DistancePips` | Distância adicional necessária para o quinto pedido. | `600` |
| `Level5DistancePips` | Distância adicional necessária para o sexto pedido (se permitido). | `700` |
| `TakeProfitCurrency` | Lucro não realizado (moeda da conta) que fecha todo o grupo. | `200` |
| `StopLossCurrency` | Perda não realizada (moeda da conta) que força uma saída de emergência. | `-500` |
| `CandleType` | Prazo usado para avaliações (velas padrão de 1 minuto). | `TimeFrame(1m)` |

> **Conversão de pip** – cada distância é multiplicada pela etapa de preço do instrumento (`PriceStep` ou `MinPriceStep`). Para símbolos cotados em pips fracionários, ajuste os valores de acordo.

## Notas e recomendações

- A implementação reflete o EA original, incluindo sua suposição de que apenas uma cesta direcional está ativa por vez. A abertura de posições simultaneamente em ambas as direções fará com que cada lado seja gerenciado de forma independente.
- Como a estratégia reage apenas no fechamento da vela, escolha um período de tempo que corresponda à capacidade de resposta desejada. Prazos mais baixos emulam mais de perto o comportamento do nível de tick.
- As técnicas Martingale amplificam o risco. Sempre faça backtest com modelos realistas de slippage e comissão e defina níveis de stop conservadores antes de ativar a estratégia em mercados reais.
- A estratégia ainda não cria uma porta Python. Somente a implementação de alto nível do C# é incluída conforme solicitado.
