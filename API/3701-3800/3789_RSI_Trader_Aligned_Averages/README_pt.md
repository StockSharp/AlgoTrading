# RSI Estratégia de médias alinhadas ao trader
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Resumo
Esta estratégia reproduz o Expert Advisor "RSI trader" MetaTrader. Ele alinha dois filtros de tendência – médias móveis de preços e médias suavizadas RSI – para entrar na direção da tendência dominante e sair quando os filtros divergem (regime lateral). A porta StockSharp funciona em qualquer instrumento com suporte a dados de velas e o padrão é velas horárias como na descrição original.

## Como funciona
1. Calcule RSI com o período especificado por **RSI Período** (padrão 14).
2. Suavize o fluxo RSI com duas médias móveis simples: uma curta (**Short RSI MA**) e uma longa (**Long RSI MA**).
3. Preços de fechamento suaves com duas médias móveis: um MA simples curto (**MA de preço curto**) e um MA longo ponderado linearmente (**MA de preço longo**).
4. Gere sinais apenas em velas finalizadas:
   - **Longo** – ambas as médias curtas (preço e RSI) estão acima de suas contrapartes longas.
   - **Curto** – ambas as médias curtas estão abaixo de suas contrapartes longas.
   - **Lateralmente** – as médias discordam (uma indica tendência de alta e a outra de baixa). Quando isso ocorre, qualquer posição aberta é fechada.
5. Os pedidos são emitidos com `BuyMarket` / `SellMarket`. As posições opostas são achatadas antes de entrar em uma nova direção.

## Parâmetros
| Nome | Descrição | Padrão | Otimizável |
| --- | --- | --- | --- |
| `RSI Period` | RSI comprimento de cálculo. | 14 | Sim (7…28, passo 1) |
| `Short Price MA` | Comprimento da média móvel simples curta do preço. | 9 | Sim (5…20, etapa 1) |
| `Long Price MA` | Comprimento da longa média móvel ponderada linear do preço. | 45 | Sim (30…90, etapa 5) |
| `Short RSI MA` | Comprimento da média de suavização curta aplicada a RSI. | 9 | Sim (5…20, etapa 1) |
| `Long RSI MA` | Comprimento da média de suavização longa aplicada a RSI. | 45 | Sim (30…90, etapa 5) |
| `Candle Type` | Tipo de dados usado para velas. O padrão é o período de 1 hora. | H1 | Não |

## Notas
- A negociação só é realizada quando todos os indicadores são formados.
- O EA original usava configurações de lotes e deslizamento. StockSharp usa a propriedade de estratégia `Volume` para o tamanho do pedido e deixa o gerenciamento do deslizamento de execução para o adaptador comercial.
- Nenhum stop-loss ou take-profit integrado é definido; as saídas dependem da detecção lateral. Gerenciamento de risco adicional pode ser adicionado externamente.
- Os gráficos traçam preços e RSI médias móveis quando o serviço de gráficos está disponível.
