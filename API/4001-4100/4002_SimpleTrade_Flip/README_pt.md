# Estratégia Flip SimpleTrade
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
- StockSharp porta do consultor especialista MetaTrader 4 **SimpleTrade.mq4** (também conhecido como "neroTrade").
- Projetado para negociação de símbolo único no período configurado por meio do parâmetro `CandleType`.
- Sempre mantém no máximo uma posição aberta e muda de direção na abertura de cada nova barra.

## Lógica de negociação
1. Cada vez que uma nova vela se torna ativa, a estratégia compara o preço de abertura da vela com o preço de abertura da vela `LookbackBars` períodos mais antiga.
2. Se a nova abertura for estritamente superior à referência histórica, todas as posições existentes serão fechadas e uma nova ordem longa de mercado com `TradeVolume` lotes será enviada.
3. Caso contrário (abertura é igual ou inferior), a estratégia fecha quaisquer posições existentes e abre uma posição curta de mercado do mesmo tamanho.
4. O parâmetro `StopLossPoints` reflete a configuração `stop` original do EA. Quando `PriceStep` e `StopLossPoints` do título estão disponíveis, a estratégia converte o valor em uma distância absoluta e o encaminha para `StartProtection`, permitindo que StockSharp mantenha as ordens de stop-loss de proteção automaticamente.
5. As aberturas de velas são rastreadas usando a assinatura de velas de alto nível API. As velas finalizadas preenchem a lista do histórico, enquanto a vela ativa aciona a decisão uma vez por barra.

## Parâmetros
| Parâmetro | Descrição | Padrão |
|-----------|-------------|---------|
| `TradeVolume` | Tamanho base do pedido expresso em lotes. Deve ser positivo. | `1` |
| `StopLossPoints` | Distância de parada protetora nos pontos do instrumento. Defina como `0` para desativar o stop loss automático. | `120` |
| `LookbackBars` | Número de barras utilizadas para comparação de preços abertos. Um valor de `3` reproduz `Open[0]` vs `Open[3]` do código original. | `3` |
| `CandleType` | Período de tempo (como `DataType`) a partir do qual as velas são solicitadas. Controla quando novos sinais aparecem. | `1 hour timeframe` |

## Notas de implementação
- Usa o fluxo de trabalho `SubscribeCandles(...).Bind(...)` de alto nível, para que a estratégia permaneça leve e reaja a velas históricas e ativas.
- `StartProtection` é invocado uma vez durante `OnStarted`. Garanta que a segurança conectada forneça `PriceStep`; caso contrário, a distância stop-loss não pode ser traduzida em preços absolutos.
- Como todas as negociações são inseridas com ordens de mercado no início de cada barra, o tratamento da derrapagem é delegado à plataforma de negociação e não há parâmetro `slippage` adicional.
- O buffer aberto histórico mantém apenas uma pequena janela contínua (`LookbackBars + 5` valores) para evitar uso desnecessário de memória.
- Nenhuma porta Python é fornecida; o diretório `CS/` contém a única implementação.

## Estrutura de arquivo
```
4002_SimpleTrade/
├──CS/
│ └── SimpleTradeFlipStrategy.cs
├── README.md
├── README_zh.md
└── README_ru.md
```
