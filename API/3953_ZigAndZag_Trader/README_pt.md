# Estratégia de Trader ZigAndZag
[English](README.md) | [Русский](README_ru.md) | [中文](README_zh.md) | [Español](README_es.md) | [Deutsch](README_de.md) | [日本語](README_ja.md)

## Visão geral
A **Estratégia ZigAndZag Trader** é a versão StockSharp do MetaTrader especialista *ZigAndZag_trader.mq4*. O sistema coloca dois detectores de balanço inspirados no ZigZag:

1. Um **ZigZag** de longo prazo (configurado por `TrendDepth`) rastreia a tendência primária marcando os principais altos e baixos do balanço.
2. Um **ZigZag de curto prazo** (configurado por `ExitDepth`) identifica o pivô de oscilação mais recente dentro dessa tendência e monitora o preço ponderado (`(5×Close + 2×Open + High + Low) / 9`).

O robô abre negociações apenas quando o preço se afasta do último pivô de oscilação na direção da tendência dominante e fecha posições quando o preço ponderado rompe esse pivô contra a tendência. Isso reproduz o comportamento do especialista MetaTrader original que lê os buffers 4–6 do indicador `ZigAndZag` personalizado.

## Lógica de negociação
- **Detecção de tendência** – quando o ZigZag de longo prazo confirma uma nova mínima, a tendência é considerada *para cima*; uma nova máxima vira para *baixo*.
- **Acompanhamento de oscilação** – cada pivô de curto prazo redefine o estado interno e armazena o preço ponderado dessa oscilação.
- **Condições de entrada**
  - Tendência de alta + último pivô é baixo: compre quando o preço ponderado subir acima do pivô armazenado em pelo menos um pip.
  - Tendência de baixa + último pivô é alta: venda quando o preço ponderado cair abaixo do pivô armazenado em pelo menos um pip.
- **Condição de saída** – se o preço retroceder através do pivô armazenado enquanto a tendência discorda da oscilação ativa, todas as posições abertas serão fechadas.
- **Limitação de pedidos** – o tamanho absoluto total da posição é limitado por `MaxOrders × Volume`. Sinais adicionais são ignorados quando esse limite é atingido.

## Parâmetros
| Parâmetro | Padrão | Descrição |
|-----------|---------|-------------|
| `CandleType` | `1 Minute` | Tipo de vela usado para ambas as avaliações ZigZag. |
| `Lots` | `0.1` | Tamanho de negociação solicitado em lotes. O volume final é alinhado ao passo de volume do instrumento. |
| `TrendDepth` | `3` | Lookback (em velas) do ZigZag de longo prazo que define a tendência. |
| `ExitDepth` | `3` | Lookback (em velas) do ZigZag de curto prazo que produz entradas e saídas oscilantes. |
| `MaxOrders` | `1` | Número máximo de ordens/unidades de posição simultâneas. |
| `StopLossPips` | `0` | Distância protetora de stop-loss em pips (`0` desativa o stop). |
| `TakeProfitPips` | `0` | Distância de lucro em pips (`0` desativa a meta). |

## Gestão de risco
`StartProtection` é ativado automaticamente. Quando a distância de stop-loss ou take-profit é definida para um valor superior a zero, as ordens de proteção fixas são anexadas a cada ordem de mercado usando a distância do pip fornecida e o tamanho do tick do instrumento.

## Visualização
A estratégia desenha castiçais e executa negociações na área padrão do gráfico. Nenhum indicador personalizado é plotado porque a lógica de entrada e saída usa rastreadores ZigZag internos.

## Notas
- A fórmula de preço ponderado é idêntica ao indicador MetaTrader e evita o acesso direto ao buffer do indicador.
- O limite de rompimento é igual a um pip do instrumento, refletindo o código original que exigia que a movimentação excedesse o spread atual.
- O port mantém todos os comentários e registros em inglês, conforme exigido pelas diretrizes do projeto.
